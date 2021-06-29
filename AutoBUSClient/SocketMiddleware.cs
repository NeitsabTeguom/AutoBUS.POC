using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using EzSockets;

namespace AutoBUSClient
{
    class SocketMiddleware
	{
		private readonly EzSocket socket;

		public SocketMiddleware(string ip, int port)
		{
			// https://github.com/RonenNess/EzSockets
			// null as ip will use localhost
			this.socket = new EzSocket(ip, port, new EzEventsListener()
			{
				OnConnectionClosedHandler = (EzSocket sock) =>
				{
					Console.WriteLine("Connection Closed!");
				},
				OnMessageReadHandler = (EzSocket sock, byte[] buff) =>
				{
					Console.WriteLine("Read message!");
				},
				OnMessageSendHandler = (EzSocket sock, byte[] data) =>
				{
					Console.WriteLine("Sent Data!");
				},
			});

			// here we also start reading messages loop
			this.socket.StartReadingMessages();
		}

		public void Send()
		{
			// send data to server
			this.socket.SendMessage("How are you today?");
		}
	}


}
