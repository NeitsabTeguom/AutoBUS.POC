using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUS.Sockets
{
    /// <summary>
    /// Listen and accept new connections.
    /// </summary>
    public class SocketListener
    {
        public Config<Config.ServiceConfigMain> config { get; private set; } = new Config<Config.ServiceConfigMain>();

        /// <summary>
        /// The class that handle socket events.
        /// </summary>
        public IEventsListener EventsListener { get; private set; }

        // listener to bind and accept connections on port
        TcpListener _listener;

        /// <summary>
        /// Get if this listener is currently listening.
        /// </summary>
        public bool IsListening { get; private set; }

        /// <summary>
        /// Port we are currently listening to (0 if not listening).
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Create the sockets listener.
        /// </summary>
        /// <param name="eventsListener">Object to handle socket events.</param>
        public SocketListener(IEventsListener eventsListener)
        {
            EventsListener = eventsListener;
        }

        /// <summary>
        /// Close socket listener.
        /// </summary>
        ~SocketListener()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Server.Close();
                _listener = null;
            }
        }

        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// Listen on port and accept connections.
        /// </summary>
        /// <param name="port">Port to listen to.</param>
        public void Listen()
        {
            // create listener and start
            this.Port = config.sc.Broker.Port;

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            string ListenHost = Dns.GetHostName();
            if (this.config.sc.Broker.ListenHost != null && this.config.sc.Broker.ListenHost.Trim() != "")
            {
                ListenHost = this.config.sc.Broker.ListenHost;
            }
            IPHostEntry ipHostInfo = Dns.GetHostEntry(ListenHost);
            IPAddress ipAddress = ipHostInfo.AddressList[ipHostInfo.AddressList.Length-1];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, this.Port);

            // Create a TCP/IP socket.
            Socket listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(this.config.sc.Broker.ListenBacklog);
                // accept connections in endless loop until set to false
                IsListening = true;
                while (IsListening)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

                listener.Close();
                listener.Dispose();
                listener = null;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            var sock = new SocketClient(handler, EventsListener);
            sock.StartReadingMessages();
        }

        /// <summary>
        /// Listen on port and accept connections in background.
        /// </summary>
        public void ListenAsync()
        {
            Task.Factory.StartNew(() =>
            {
                Listen();
            });
        }

        /// <summary>
        /// Stop listening to port.
        /// </summary>
        public void StopListening()
        {
            IsListening = false;
        }
    }
}
