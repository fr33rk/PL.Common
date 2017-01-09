using System;
using System.Threading.Tasks;

namespace PL.Common.Socket
{
	/// <summary>Event args send when receiving data on a socket.
	/// </summary>
	public class SocketDataReceivedEventArgs : EventArgs
	{
		public SocketDataReceivedEventArgs(string data)
		{
			Data = data;
		}

		/// <summary>Gets the received data.</summary>
		public string Data { get; private set; }
	}

	/// <summary>Interface for classes around a TCP/IP socket client.
	/// </summary>
	public interface ISender
	{
		/// <summary>Starts the sender.
		/// </summary>
		void Start();

		/// <summary>Stops this instance.
		/// </summary>
		void Stop();

		/// <summary>Write to the connection asynchronous.
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <returns>true, when the message was send successfully</returns>
		Task<bool> WriteAsync(string message);

		/// <summary>Occurs when data has been received on the socket.
		/// </summary>
		event EventHandler<SocketDataReceivedEventArgs> OnDataReceived;

		/// <summary>Occurs when the sender makes contact to a server.
		/// </summary>
		event EventHandler OnConnect;

		/// <summary>Occurs when the client is disconnected unexpected. (Server connection lost)
		/// </summary>
		event EventHandler OnDisconnect;
	}
}