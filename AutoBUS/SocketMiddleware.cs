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

		public enum SocketType
        {
			Server,
			Client
        }

		public SocketType socketType { get; private set; }

		/// <summary>
		/// Socket client
		/// </summary>
		private EzSocket client;

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

		public class UserData
        {
			public UInt16 NegociateVersion { get; set; } = 1;
        }

		private Broker broker;

		public SocketMiddleware(
			SocketType socketType,
			int port, 
			string ip = null)
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

			this.socketType = socketType;
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						this.server = new EzSocketListener(this.listener);
						break;
					}
				case SocketType.Client:
					{ 
						this.client = new EzSocket(ip, port, this.listener);
						break;
					}
			}
		}

		/// <summary>
		/// Satrt listening on port
		/// </summary>
		public void Start()
		{
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						Console.WriteLine("Listener running...");
						this.server?.ListenAsync(this.port);
						break;
					}
				case SocketType.Client:
					{
						Console.WriteLine("Client running...");
						this.client?.StartReadingMessages();

						if (this.socketType == SocketType.Client)
						{
							this.broker.ms.VersionCheck(-1);
						}
						break;
					}
			}
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

			switch (this.socketType)
			{
				case SocketType.Server:
					{
						this.server?.StopListening();
						break;
					}
				case SocketType.Client:
					{
						this.client?.StopReadingMessages();
						break;
					}
			}
		}

		/// <summary>
		/// Send data to connected client (alive)
		/// </summary>
		/// <param name="SocketId">Id of socket to send</param>
		/// <param name="data">Data to send</param>
		/// <returns></returns>
		public bool Send(long SocketId, byte[] data)
		{
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						if (this.sockets.ContainsKey(SocketId))
						{
							this.sockets[SocketId].SendMessage(data);
							return true;
						}
						break;
					}
				case SocketType.Client:
					{
						this.client.SendMessage(data);
						return true;
					}
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
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						if (this.sockets.ContainsKey(SocketId))
						{
							this.sockets[SocketId].StopReadingMessages();
							this.sockets[SocketId].Close();
							this.sockets.Remove(SocketId);
							return true;
						}
						break;
					}
				case SocketType.Client:
					{
						this.client.Close();
						return true;
					}
			}
			return false;
        }

		/// <summary>
		///  Server only
		/// </summary>
		/// <param name="SocketId"></param>
		/// <returns></returns>
		public UserData GetSocketData(long SocketId)
		{
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						if (this.sockets.ContainsKey(SocketId))
						{
							return (UserData)(this.sockets[SocketId].UserData ?? new UserData());
						}
						break;
					}
				case SocketType.Client:
					{
						return (UserData)(this.client.UserData ?? new UserData());
					}
			}
			return null;
		}

		/// <summary>
		/// Server only
		/// </summary>
		/// <param name="SocketId"></param>
		/// <param name="userData"></param>
		public void SetSocketData(long SocketId, UserData userData)
		{
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						this.sockets[SocketId].UserData = userData;
						break;
					}
				case SocketType.Client:
					{
						this.client.UserData = userData;
						break;
					}
			}
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
			this.broker.TakeIn(socket.SocketId, data);
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
