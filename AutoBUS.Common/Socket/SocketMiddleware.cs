using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using AutoBUS.Sockets;

namespace AutoBUS
{
    public class SocketMiddleware
	{
		private Broker broker;

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

		public SocketMiddleware(Broker broker)
		{
			this.broker = broker;

			this.checkTimer = new Timer();
			this.checkTimer.Enabled = false;
			this.checkTimer.Interval = this.broker.configManager.sc.Broker.CheckInterval;
			this.checkTimer.AutoReset = true;
			this.checkTimer.Elapsed += CheckTimer_Elapsed;


			// create new server with default event listener and add some events
			this.listener = new EventsListener()
			{
				OnNewConnectionHandler = OnNewConnectionHandler,
				OnConnectionClosedHandler = OnConnectionClosedHandler,
				OnMessageReadHandler = OnMessageReadHandler,
				OnMessageSendHandler = OnMessageSendHandler,
				OnExceptionHandler = OnExceptionHandler
			};

			this.Init();
		}

		private bool Init()
        {
			switch (this.broker.brokerType)
			{
				case Broker.BrokerTypes.Federator:
					{
						this.server = new SocketListener(this.listener);
						return this.server.IsListening;
					}
				case Broker.BrokerTypes.Worker:
					{
						this.client = new SocketClient(this.listener, this.broker);
						return this.client.Connected;
					}
			}
			return false;
		}

        private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
			this.checkTimer.Stop();
			try
			{
				switch (this.broker.brokerType)
				{
					case Broker.BrokerTypes.Federator:
						{
							if (this.server == null || !this.server.IsListening)
							{
								try
								{
									this.Stop();
									if(this.Init())
									{
										this.Start();
									}
								}
								catch { }
							}
							break;
						}
					case Broker.BrokerTypes.Worker:
						{
							if (this.client == null || !this.client.Connected)
							{
								try
								{
									this.Stop();
									if(this.Init())
									{
										this.Start();
									}
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
				switch (this.broker.brokerType)
				{
					case Broker.BrokerTypes.Federator:
						{
							this.server?.ListenAsync(this.broker);
							Console.WriteLine("Listener running...");
							break;
						}
					case Broker.BrokerTypes.Worker:
						{
							this.client?.StartReadingMessages();
							Console.WriteLine("Client running...");

							this.client?.Infos.messages.VersionCheck(-1);

							break;
						}
				}
            }
            catch 
			{ 
			}

			if (this.broker.configManager.sc.Broker.CheckInterval > 0)
			{
				this.checkTimer.Start();
			}
		}

        /// <summary>
        /// Stop listening
        /// </summary>
        public void Stop()
		{

			if (this.broker.configManager.sc.Broker.CheckInterval > 0)
			{
				this.checkTimer.Stop();
			}

			long[] keys = this.sockets.Keys.ToArray();
			foreach (long SocketId in keys)
            {
				this.Close(SocketId);
            }

			switch (this.broker.brokerType)
			{
				case Broker.BrokerTypes.Federator:
					{
						this.CloseAll();
						this.server?.StopListening();
						break;
					}
				case Broker.BrokerTypes.Worker:
					{
						this.client?.StopReadingMessages();
						this.client?.Close();
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
			switch (this.broker.brokerType)
			{
				case Broker.BrokerTypes.Federator:
					{
						if (this.sockets.ContainsKey(SocketId))
						{
							return this.sockets[SocketId].SendMessage(data);
						}
						break;
					}
				case Broker.BrokerTypes.Worker:
					{
						return this.client.SendMessage(data);
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
			switch (this.broker.brokerType)
			{
				case Broker.BrokerTypes.Federator:
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
				case Broker.BrokerTypes.Worker:
					{
						this.client.StopReadingMessages();
						this.client.Close();
						return true;
					}
			}
			return false;
        }

		public void CloseAll()
        {
			foreach (KeyValuePair<long, SocketClient> sc in this.sockets)
			{
				sc.Value.StopReadingMessages();
				sc.Value.Close();
			}
			this.sockets.Clear();
		}

		/// <summary>
		///  
		/// </summary>
		/// <param name="SocketId"></param>
		/// <returns></returns>
		public SocketClient GetSocketClient(long SocketId)
		{
			switch (this.broker.brokerType)
			{
				case Broker.BrokerTypes.Federator:
					{
						if (this.sockets.ContainsKey(SocketId))
						{
							return this.sockets[SocketId];
						}
						break;
					}
				case Broker.BrokerTypes.Worker:
					{
						return this.client;
					}
			}
			return null;
		}

		private void OnNewConnectionHandler(SocketClient socket)
        {
			this.sockets.Add(socket.SocketId, socket);
			Console.WriteLine("Connected!");
			//socket.StartReadingMessages(); // <-- this will make the new socket listen to incoming messages and trigger events.

			// TODO : reprise des fichiers messages non envoyés
		}

		private void OnConnectionClosedHandler(SocketClient socket)
		{
			this.sockets.Remove(socket.SocketId);
			Console.WriteLine("Connection Closed!");
		}

		private void OnMessageReadHandler(SocketClient socket, byte[] data)
		{
			Console.WriteLine("Read message!");
			this.broker.TakeIn(socket, data);
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
