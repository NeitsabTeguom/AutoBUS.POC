using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using EzSockets;

namespace AutoBUS
{
    public class SocketMiddleware
	{
		// https://github.com/RonenNess/EzSockets

		private int port;

		/// <summary>
		/// Socket server
		/// </summary>
		private EzSocketListener server;

		/// <summary>
		/// Socket events
		/// </summary>
		private EzEventsListener listener;

		/// <summary>
		/// Sockets clients list
		/// </summary>
		private Dictionary<long, EzSocket> sockets = new Dictionary<long, EzSocket>();

		private Broker broker;

		public SocketMiddleware(int port)
		{
			this.port = port;

			// begin with broker, because it must be available before message coming
			this.broker = new Broker(this);

			// create new server with default event listener and add some events
			this.listener = new EzEventsListener()
			{
				OnNewConnectionHandler = OnNewConnectionHandler,
				OnConnectionClosedHandler = OnConnectionClosedHandler,
				OnMessageReadHandler = OnMessageReadHandler,
				OnMessageSendHandler = OnMessageSendHandler,
				OnExceptionHandler = OnExceptionHandler
			};

			this.server = new EzSocketListener(this.listener);
		}

		/// <summary>
		/// Satrt listening on port
		/// </summary>
		public void Start()
		{
			Console.WriteLine("Listener running...");
			this.server.ListenAsync(this.port);
		}

        /// <summary>
        /// Stop listening
        /// </summary>
        public void Stop()
		{
			long[] keys = this.sockets.Keys.ToArray();
			foreach (long SocketId in keys)
            {
				this.Close(SocketId);
            }
			this.server.StopListening();
		}

		/// <summary>
		/// Send data to connected client (alive)
		/// </summary>
		/// <param name="SocketId">Id of socket to send</param>
		/// <param name="data">Data to send</param>
		/// <returns></returns>
		public bool Send(long SocketId, byte[] data)
		{
			if(this.sockets.ContainsKey(SocketId))
            {
				this.sockets[SocketId].SendMessage(data);
				return true;
            }
			return false;
		}

		/// <summary>
		/// Close a connection with a client
		/// </summary>
		/// <param name="SocketId"></param>
		/// <returns></returns>
		public bool Close(long SocketId)
		{
			if (this.sockets.ContainsKey(SocketId))
			{
				this.sockets[SocketId].StopReadingMessages();
				this.sockets[SocketId].Close();
				this.sockets.Remove(SocketId);
				return true;
			}
			return false;
        }

		private void OnNewConnectionHandler(EzSocket socket)
        {
			this.sockets.Add(socket.SocketId, socket);
			Console.WriteLine("Connected!");
			socket.StartReadingMessages(); // <-- this will make the new socket listen to incoming messages and trigger events.
		}

		private void OnConnectionClosedHandler(EzSocket socket)
		{
			this.sockets.Remove(socket.SocketId);
			Console.WriteLine("Connection Closed!");
		}

		private void OnMessageReadHandler(EzSocket socket, byte[] data)
		{
			this.broker.Deliver(socket.SocketId, data);
			Console.WriteLine("Read message!");
		}

		private void OnMessageSendHandler(EzSocket socket, byte[] data)
		{
			Console.WriteLine("Sent message!");
		}

		private ExceptionHandlerResponse OnExceptionHandler(EzSocket socket, Exception ex)
		{
			Console.WriteLine("Error! " + ex.ToString());
			return ExceptionHandlerResponse.CloseSocket;
		}
}
}
