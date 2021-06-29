using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Text;
using EzSockets;

namespace AutoBUS
{
    class SocketMiddleware
	{
		public SocketMiddleware()
		{
		}

		// https://github.com/RonenNess/EzSockets
		// create new server with default event listener and add some events
		private EzSocketListener server = new EzSocketListener(new EzEventsListener()
		{
			OnNewConnectionHandler = (EzSocket socket) =>
			{
				Console.WriteLine("Connected!");
				socket.SendMessage("hello!");
				socket.StartReadingMessages(); // <-- this will make the new socket listen to incoming messages and trigger events.
			},
			OnConnectionClosedHandler = (EzSocket socket) =>
			{
				Console.WriteLine("Connection Closed!");
			},
			OnMessageReadHandler = (EzSocket socket, byte[] data) =>
			{
				Console.WriteLine("Read message!");
				socket.SendMessage(data);
			},
			OnMessageSendHandler = (EzSocket socket, byte[] data) =>
			{
				Console.WriteLine("Sent message!");
			},
			OnExceptionHandler = (EzSocket socket, Exception ex) =>
			{
				Console.WriteLine("Error! " + ex.ToString());
				return ExceptionHandlerResponse.CloseSocket;
			}
		});

		public void Start(int port)
		{
			this.server.ListenAsync(port);
		}

		public void Stop()
		{
			this.server.StopListening();
		}
	}
}
