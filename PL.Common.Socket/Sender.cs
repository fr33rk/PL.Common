using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using PL.Logger;

namespace PL.Common.Socket
{
	/// <summary>Wrapper around a TcpClient.
	///
	/// </summary>
	public class Sender : ISender
	{
		#region Fields

		private readonly ILogFile mLogFile;
		private TcpClient mClient;
		private readonly Timer mConnectionTimer;
		private readonly Timer mAliveCheckTimer;
		private const int cDefaultBufferSize = 256;
		private readonly string mServerAddress;
		private readonly int mServerPort;
		private readonly string mAliveMessage;

		#endregion Fields

		#region Constructor(s)

		/// <summary>Initializes a new instance of the <see cref="Sender" /> class.
		/// </summary>
		/// <param name="logFile">The log file.</param>
		/// <param name="serverAddress">The server address.</param>
		/// <param name="serverPort">The server port.</param>
		/// <param name="retryInterval">The retry interval.</param>
		/// <param name="aliveCheckInterval">The alive check interval.</param>
		/// <param name="aliveMessage">The alive message.</param>
		/// <exception cref="System.InvalidOperationException">Retry interval is not allowed to be less or equal to 0.</exception>
		public Sender(ILogFile logFile, string serverAddress, int serverPort, int retryInterval, int aliveCheckInterval, string aliveMessage)
		{
			mLogFile = logFile;
			mClient = new TcpClient();

			mServerAddress = serverAddress;
			mServerPort = serverPort;
			mAliveMessage = aliveMessage;

			if (retryInterval > 0)
			{
				mConnectionTimer = new Timer(retryInterval);
				mConnectionTimer.Elapsed += TryConnectAsync;
			}
			else
			{
				throw new InvalidOperationException("Retry interval is not allowed to be less or equal to 0.");
			}

			if (aliveCheckInterval > 0)
			{
				mAliveCheckTimer = new Timer(aliveCheckInterval);
				mAliveCheckTimer.Elapsed += TestConnection;
			}
		}

		/// <summary>Initializes a new instance of the <see cref="Sender"/> class.</summary>
		/// <param name="logFile">The log file.</param>
		/// <param name="serverAddress">The server address.</param>
		/// <param name="serverPort">The server port.</param>
		/// <param name="retryInterval">The retry interval.</param>
		public Sender(ILogFile logFile, string serverAddress, int serverPort, int retryInterval)
			: this(logFile, serverAddress, serverPort, retryInterval, -1, string.Empty)
		{
			// Nothing additional to do here.
		}

		#endregion Constructor(s)

		#region Events

		/// <summary>Occurs when data has been received on the socket.
		/// </summary>
		public event EventHandler<SocketDataReceivedEventArgs> OnDataReceived;

		/// <summary>Occurs when the sender makes contact to a server.
		/// </summary>
		public event EventHandler OnConnect;

		/// <summary>Occurs when the client is disconnected unexpected. (Server connection lost)
		/// </summary>
		public event EventHandler OnDisconnect;

		#endregion Events

		#region Starting and stopping

		/// <summary>Starts the sender.</summary>
		/// <exception cref="System.InvalidOperationException">Sender started before it has been prepared.</exception>
		public void Start()
		{
			try
			{
				// Start the connection timer. In the elapsed event the system tries to connect to the server.
				mConnectionTimer.Start();
			}
			catch (NullReferenceException)
			{
				throw new InvalidOperationException("Sender started before it has been prepared.");
			}
		}

		/// <summary>Stops this instance.
		/// </summary>
		public void Stop()
		{
			if (mClient.Connected)
			{
				mAliveCheckTimer?.Stop();
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
					mConnectionTimer.Stop();
					await mClient.ConnectAsync(mServerAddress, mServerPort);

					if (mClient.Connected)
					{
						mAliveCheckTimer.Start();

						mLogFile?.Info(
							$"Connected to {((IPEndPoint)mClient.Client.RemoteEndPoint).Address}:{((IPEndPoint)mClient.Client.RemoteEndPoint).Port}");

						// Check if the event needs to be raised.
						OnConnect?.Invoke(this, EventArgs.Empty);

						// Start reading from the connection
						var stream = mClient.GetStream();
						var buffer = new byte[cDefaultBufferSize];

						ReadAsync(stream, buffer);
					}
				}
				catch (Exception ex)
				{
					mLogFile?.Error($"Sender::TryConnectAsync - {ex.Message}");
					mConnectionTimer.Start();
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
				if (mClient.Connected && !await WriteAsync(mAliveMessage))
				{
					mLogFile?.Debug("Disconnect detected");

					// Close the socket to clean up. Failing to do so will lead to the error
					//  "A connect request was made on an already connected socket" when we try to reconnect.
					mClient.Close();

					mAliveCheckTimer.Stop();
					mConnectionTimer.Stop();

					OnDisconnect?.Invoke(this, EventArgs.Empty);

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

			OnDisconnect?.Invoke(this, EventArgs.Empty);

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
				var amountRead = await stream.ReadAsync(buffer, 0, buffer.Length);

				if (amountRead > 0)
				{
					var text = Encoding.UTF8.GetString(buffer, 0, amountRead);

					mLogFile?.Debug($"Received: {text}");

					OnDataReceived?.Invoke(this, new SocketDataReceivedEventArgs(text));

					// Continue reading
					ReadAsync(stream, buffer);
				}
				else
				{
					mLogFile?.Info("No data found. The connection seems to be closed.");
					ResetConnection();
				}
			}
			catch (ObjectDisposedException)
			{
				// Occurs when the connection is closed.
			}
			catch (Exception e)
			{
				mLogFile?.Error(e.Message);
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