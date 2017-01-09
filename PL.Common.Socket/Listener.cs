using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PL.Logger;

namespace PL.Common.Socket
{
	public class Listener
	{
		#region Fields

		private readonly ILogFile mLogFile;
		private readonly TcpListener mTcpListener;
		private readonly List<TcpClient> mClientConnections;
		private bool mStarted;
		private const int cBufferSize = 256;

		#endregion Fields

		#region Constructor(s)

		/// <summary>Initializes a new instance of the <see cref="Listener"/> class.
		/// </summary>
		/// <param name="logFile">The log file to use.</param>
		/// <param name="listeningPort">The listening port.</param>
		public Listener(ILogFile logFile, int listeningPort)
		{
			mLogFile = logFile;
			mTcpListener = new TcpListener(IPAddress.Any, listeningPort);
			mClientConnections = new List<TcpClient>();
			mStarted = false;
		}

		#endregion Constructor(s)

		#region Starting and stopping

		/// <summary>Start listening
		/// </summary>
		public void Start()
		{
			mTcpListener.Start();

			mStarted = true;

			mLogFile.Info($"Started listening on {((IPEndPoint) mTcpListener.LocalEndpoint).Port}");
			Listen();
		}

		/// <summary>Close all connections and stop listening.
		/// </summary>
		public void Stop()
		{
			if (mStarted)
			{
				foreach (var tcpClient in mClientConnections)
				{
					try
					{
						tcpClient.Close();
					}
					catch (Exception e)
					{
						mLogFile.Error($"Listener::Stop - {e.Message}");
					}
				}

				mClientConnections.Clear();

				mTcpListener.Stop();

				mLogFile.Info($"Stopped listening on {((IPEndPoint) mTcpListener.LocalEndpoint).Port}");
				mStarted = false;
			}
		}

		#endregion Starting and stopping

		#region Listening

		/// <summary>Listen for new connections
		/// </summary>
		public async void Listen()
		{
			try
			{
				//blocks (asynchronous) until a client has connected to the server
				var tcpClient = await mTcpListener.AcceptTcpClientAsync();

				mLogFile.Info(
					$"Connection made with: {((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address} on port {((IPEndPoint) tcpClient.Client.RemoteEndPoint).Port}");

				var stream = tcpClient.GetStream();
				var buffer = new Byte[cBufferSize];

				mClientConnections.Add(tcpClient);

				ListenForData(tcpClient, stream, buffer);

				// Continue listening for new connections.
				Listen();
			}
			catch (ObjectDisposedException)
			{
				// Occurs when the connection is closed.
			}
			catch (Exception e)
			{
				mLogFile.Error(e.Message);
			}
		}

		#endregion Listening

		#region Receiving and sending data

		/// <summary>Listens for data.
		/// </summary>
		/// <param name="tcpClient">the client</param>
		/// <param name="stream">The stream.</param>
		/// <param name="buffer">The buffer.</param>
		public async void ListenForData(TcpClient tcpClient, NetworkStream stream, byte[] buffer)
		{
			try
			{
				var amountRead = await stream.ReadAsync(buffer, 0, buffer.Length);

				if (amountRead > 0)
				{
					var text = Encoding.UTF8.GetString(buffer, 0, amountRead);

					mLogFile.Debug($"Received: {text}");

					ListenForData(tcpClient, stream, buffer);
				}
				else
				{
					mLogFile.Info("No data found. The connection seems to be closed.");
					mClientConnections.Remove(tcpClient);
				}
			}
			catch (ObjectDisposedException)
			{
				// Occurs when the connection is closed.
			}
			catch (Exception e)
			{
				mLogFile.Error(e.Message);
			}
		}

		/// <summary>Broadcast a message to all connected clients.
		/// </summary>
		/// <param name="message">The message.</param>
		public async void BroadCastMessage(string message)
		{
			mLogFile.Info($"Broadcast message: {message}");

			foreach (var tcpClient in mClientConnections)
			{
				if (tcpClient.Connected)
				{
					var stream = tcpClient.GetStream();

					var buffer = Encoding.UTF8.GetBytes(message);
					var messageSize = Encoding.UTF8.GetByteCount(message);

					await stream.WriteAsync(buffer, 0, messageSize);
				}
			}
		}

		#endregion Receiving and sending data
	}
}