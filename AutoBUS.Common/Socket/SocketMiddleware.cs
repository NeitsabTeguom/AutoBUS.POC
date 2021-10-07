using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using AutoBUS.Sockets;

namespace AutoBUS
{
    public class SocketMiddleware
	{
		public enum SocketType
        {
			Server,
			Client
        }

		public SocketType socketType { get; private set; }

		/// <summary>
		/// Socket client
		/// </summary>
		private SocketClient client;

		/// <summary>
		/// Socket server
		/// </summary>
		private SocketListener server;

		/// <summary>
		/// Socket events
		/// </summary>
		private EventsListener listener;

		/// <summary>
		/// Sockets clients list
		/// </summary>
		private Dictionary<long, SocketClient> sockets = new Dictionary<long, SocketClient>();

		private Timer checkTimer;

		public class SocketInfos
        {
			public UInt16? NegociateVersion { get; set; } = null;

			public Messages messages = new Messages();
        }

		private Broker broker;

		public SocketMiddleware(
			SocketType socketType,
			double checkInterval = 1000)
		{

			this.checkTimer = new Timer();
			this.checkTimer.Enabled = false;
			this.checkTimer.Interval = checkInterval;
			this.checkTimer.AutoReset = true;
			this.checkTimer.Elapsed += CheckTimer_Elapsed;

			// begin with broker, because it must be available before message coming
			this.broker = new Broker(this);

			// create new server with default event listener and add some events
			this.listener = new EventsListener()
			{
				OnNewConnectionHandler = OnNewConnectionHandler,
				OnConnectionClosedHandler = OnConnectionClosedHandler,
				OnMessageReadHandler = OnMessageReadHandler,
				OnMessageSendHandler = OnMessageSendHandler,
				OnExceptionHandler = OnExceptionHandler
			};

			this.socketType = socketType;

			this.Init();
		}

		private void Init()
        {
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						this.server = new SocketListener(this.listener);
						break;
					}
				case SocketType.Client:
					{
						this.client = new SocketClient(this.listener);
						break;
					}
			}
		}

        private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
			this.checkTimer.Stop();
			try
			{
				switch (this.socketType)
				{
					case SocketType.Server:
						{
							if (this.server == null || !this.server.IsListening)
							{
								try
								{
									this.server.StopListening();
									this.Init();
									this.Start();
								}
								catch { }
							}
							break;
						}
					case SocketType.Client:
						{
							if (this.client == null || !this.client.Connected)
							{
								try
								{
									this.client.Close();
									this.Init();
									this.Start();
								}
								catch { }
							}
							break;
						}
				}
            }
            catch { }
			this.checkTimer.Start();
		}

        /// <summary>
        /// Satrt listening on port
        /// </summary>
        public void Start()
		{
			try
			{
				switch (this.socketType)
				{
					case SocketType.Server:
						{
							this.server?.ListenAsync();
							Console.WriteLine("Listener running...");
							break;
						}
					case SocketType.Client:
						{
							this.client?.StartReadingMessages();
							Console.WriteLine("Client running...");

							if (this.socketType == SocketType.Client)
							{
								this.GetSocketInfo(-1).messages.VersionCheck(this.broker, -1);
							}
							break;
						}
				}
            }
            catch 
			{ 
			}

			//this.checkTimer.Start();
		}

        /// <summary>
        /// Stop listening
        /// </summary>
        public void Stop()
		{
			this.checkTimer.Stop();

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
		public SocketInfos GetSocketInfo(long SocketId)
		{
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						if (this.sockets.ContainsKey(SocketId))
						{
							return (SocketInfos)(this.sockets[SocketId].Infos ?? new SocketInfos());
						}
						break;
					}
				case SocketType.Client:
					{
						return (SocketInfos)(this.client.Infos ?? new SocketInfos());
					}
			}
			return null;
		}

		/// <summary>
		/// Server only
		/// </summary>
		/// <param name="SocketId"></param>
		/// <param name="userData"></param>
		public void SetSocketInfo(long SocketId, SocketInfos infos)
		{
			switch (this.socketType)
			{
				case SocketType.Server:
					{
						this.sockets[SocketId].Infos = infos;
						break;
					}
				case SocketType.Client:
					{
						this.client.Infos = infos;
						break;
					}
			}
		}

		private void OnNewConnectionHandler(SocketClient socket)
        {
			this.sockets.Add(socket.SocketId, socket);
			Console.WriteLine("Connected!");
			//socket.StartReadingMessages(); // <-- this will make the new socket listen to incoming messages and trigger events.
		}

		private void OnConnectionClosedHandler(SocketClient socket)
		{
			this.sockets.Remove(socket.SocketId);
			Console.WriteLine("Connection Closed!");
		}

		private void OnMessageReadHandler(SocketClient socket, byte[] data)
		{
			Console.WriteLine("Read message!");
			this.broker.TakeIn(socket.SocketId, data);
		}

		private void OnMessageSendHandler(SocketClient socket, byte[] data)
		{
			Console.WriteLine("Sent message!");
		}

		private ExceptionHandlerResponse OnExceptionHandler(SocketClient socket, Exception ex)
		{
			Console.WriteLine("Error! " + ex.ToString());
			return ExceptionHandlerResponse.CloseSocket;
		}
}
}
