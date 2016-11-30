using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using PL.Logger;

namespace PL.Common.Socket
{
	public class SenderArgs
	{
		public string ServerAddress { get; set; }
		public int ServerPort { get; set; }
		public int RetryInterval { get; set; }
		public int AliveCheckInterval { get; set; }
		public string AliveMessage { get; set; }
	}

	public class Sender
	{
		#region Fields

		private readonly ILogFile mLogFile;
		private TcpClient mClient;
		private readonly Timer mConnectionTimer;
		private readonly Timer mAliveCheckTimer;
		private readonly SenderArgs mSenderArgs;
		private const int cDefaultBufferSize = 256;

		#endregion Fields

		#region Constructor(s)

		/// <summary>Initializes a new instance of the <see cref="Sender"/> class.
		/// </summary>
		/// <param name="logFile">The log file.</param>
		/// <param name="args">Object containing all requires external settings.</param>
		public Sender(ILogFile logFile, SenderArgs args)
		{
			mLogFile = logFile;
			mSenderArgs = args;

			mConnectionTimer = new Timer(args.RetryInterval);
			mConnectionTimer.Elapsed += TryConnectAsync;

			mAliveCheckTimer = new Timer(args.AliveCheckInterval);
			mAliveCheckTimer.Elapsed += TestConnection;

			mClient = new TcpClient();
		}

		#endregion Constructor(s)

		#region Events

		/// <summary>Occurs when data has been received on the socket.
		/// </summary>
		public event Action<string> OnDataReceived;

		/// <summary>Occurs when the sender makes contact to a server.
		/// </summary>
		public event Action OnConnect;

		/// <summary>Occurs when the client is disconnected unexpected. (Server connection lost)
		/// </summary>
		public event Action OnDisconnect;

		#endregion Events

		#region Starting and stopping

		/// <summary>Starts the sender.
		/// </summary>
		public void Start()
		{
			// Start the connection timer. In the elapsed event the system tries to connect to the server.
			mConnectionTimer.Start();
		}

		/// <summary>Stops this instance.
		/// </summary>
		public void Stop()
		{
			if (mClient.Connected)
			{
				mAliveCheckTimer.Stop();
				mClient.Close();
			}
			else
			{
				mConnectionTimer.Stop();
			}
		}

		#endregion Starting and stopping

		#region Connection

		private bool mElapsedBusyFlag;

		/// <summary>Timer event which tries the connect to the server asynchronous.
		/// </summary>
		/// <param name="source">The (timer) source.</param>
		/// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
		private async void TryConnectAsync(object source, ElapsedEventArgs e)
		{
			if (!mElapsedBusyFlag)
			{
				mElapsedBusyFlag = true;

				try
				{
					await mClient.ConnectAsync(mSenderArgs.ServerAddress, mSenderArgs.ServerPort);

					if (mClient.Connected)
					{
						mConnectionTimer.Stop();
						mAliveCheckTimer.Start();

						mLogFile.Info(
							$"Connected to {((IPEndPoint)mClient.Client.RemoteEndPoint).Address}:{((IPEndPoint)mClient.Client.RemoteEndPoint).Port}");

						// Check if the event needs to be raised.
						OnConnect?.Invoke();

						// Start reading from the connection
						NetworkStream stream = mClient.GetStream();
						byte[] buffer = new byte[cDefaultBufferSize];

						ReadAsync(stream, buffer);
					}
				}
				catch (Exception ex)
				{
					mLogFile.Error($"Sender::TryConnectAsync - {ex.Message}");
				}
				mElapsedBusyFlag = false;
			}
		}

		private bool mConnectionTestElapsedFlag;

		/// <summary>Timer event handler which tests the connection with the server.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
		private async void TestConnection(object source, ElapsedEventArgs e)
		{
			if (!mConnectionTestElapsedFlag)
			{
				mConnectionTestElapsedFlag = true;
				if (mClient.Connected && !await WriteAsync(mSenderArgs.AliveMessage))
				{
					mLogFile.Debug("Disconnect detected");

					// Close the socket to clean up. Failing to do so will lead to the error
					//  "A connect request was made on an already connected socket" when we try to reconnect.
					mClient.Close();

					mAliveCheckTimer.Stop();
					mConnectionTimer.Stop();

					OnDisconnect?.Invoke();

					// Try again to make connection.
					mClient = new TcpClient();
					mConnectionTimer.Start();
				}
				mConnectionTestElapsedFlag = false;
			}
		}

		private void ResetConnection()
		{
			// Close the socket to clean up. Failing to do so will lead to the error
			//  "A connect request was made on an already connected socket" when we try to reconnect.
			mClient.Close();

			mAliveCheckTimer.Stop();
			mConnectionTimer.Stop();

			OnDisconnect?.Invoke();

			// Try again to make connection.
			mClient = new TcpClient();
			mConnectionTimer.Start();
		}

		#endregion Connection

		#region Read/write

		/// <summary>Reads the asynchronous from the connection.
		/// </summary>
		/// <param name="stream">The socket stream.</param>
		/// <param name="buffer">The buffer to use for storing bytes.</param>
		private async void ReadAsync(NetworkStream stream, byte[] buffer)
		{
			try
			{
				int amountRead = await stream.ReadAsync(buffer, 0, buffer.Length);

				if (amountRead > 0)
				{
					string text = Encoding.UTF8.GetString(buffer, 0, amountRead);

					mLogFile.Debug($"Received: {text}");

					OnDataReceived?.Invoke(text);

					// Continue reading
					ReadAsync(stream, buffer);
				}
				else
				{
					mLogFile.Info("No data found. The connection seems to be closed.");
					ResetConnection();
				}
			}
			catch (ObjectDisposedException)
			{
				// Occurs when the connection is closed.
			}
			catch (Exception e)
			{
				mLogFile.Error(e.Message);
				ResetConnection();
			}
		}

		/// <summary>Write to the connection asynchronous.
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <returns>true, when the message was send successfully</returns>
		public async Task<bool> WriteAsync(string message)
		{
			try
			{
				var stream = mClient.GetStream();

				var buffer = Encoding.UTF8.GetBytes(message);
				var messageSize = Encoding.UTF8.GetByteCount(message);

				await stream.WriteAsync(buffer, 0, messageSize);

				return true;
			}
			catch
			{
				return false;
			}
		}

		#endregion Read/write
	}
}