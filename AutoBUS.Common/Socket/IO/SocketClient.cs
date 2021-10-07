using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUS.Sockets
{
    /// <summary>
    /// A simple socket wrapper.
    /// </summary>
    public class SocketClient
    {
        public Config<Config.ServiceConfigWorker> config { get; private set; } = new Config<Config.ServiceConfigWorker>();

        #region Members
        // State object for reading client data asynchronously
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Receive buffer.
            public byte[] buffer;

            public int frameLength
            {
                get
                {
                    return this.buffer.Length;
                }
            }

            public enum States { Length, Data }
            public States State;

            public StateObject()
            {
                this.ModifyState();
            }

            public void ModifyState(int length = -1)
            {
                if(length == -1)
                {
                    this.State = States.Length;
                    length = 4;
                }
                else
                {
                    this.State = States.Data;
                }
                this.buffer = null;
                this.buffer = new byte[length];
            }
        }

        /// <summary>
        /// Events handler.
        /// </summary>
        private IEventsListener _eventsListener;

        /// <summary>
        /// The socket object we wrap.
        /// </summary>
        public Socket Client { get; private set; }

        /// <summary>
        /// Unique socket id.
        /// </summary>
        public long SocketId { get; private set; }

        // next session id
        private static long _nextSocketId = 0;

        /// <summary>
        /// Function used to convert string to bytes array.
        /// Override this if you need different encoding.
        /// </summary>
        public static Func<string, byte[]> StringToBytes = (string str) =>
        {
            return Encoding.UTF8.GetBytes(str);
        };

        /// <summary>
        /// Function used to convert bytes array to string.
        /// Override this if you need different encoding.
        /// </summary>
        public static Func<byte[], string> BytesToString = (byte[] bytes) =>
        {
            return Encoding.UTF8.GetString(bytes);
        };

        /// <summary>
        /// Get if socket is connected.
        /// </summary>
        public bool Connected { get { return Client.Connected; } }

        /// <summary>
        /// Get if socket is reading.
        /// </summary>
        public bool Reading { get; private set; } = false;

        /// <summary>
        /// Max bytes for lentgth.
        /// </summary>
        private byte FrameLengthByteNumber = 4;


        /// <summary>
        /// Optional data you can attach to this socket.
        /// </summary>
        public object Infos;

        // did we call the closed socket event?
        bool _wasClosedEventCalled;

        /// <summary>
        /// Default IP to use when creating socket with dest ip == null.
        /// </summary>
        public static string DefaultDestIp = IPAddress.Loopback.ToString();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Handle exceptions.
        /// </summary>
        private void HandleException(Exception ex)
        {
            var ret = _eventsListener.OnException(this, ex);
            switch (ret)
            {
                case ExceptionHandlerResponse.CloseSocket:
                    Close();
                    break;

                case ExceptionHandlerResponse.Rethrow:
                    throw ex;

                case ExceptionHandlerResponse.Silence:
                    break;
            }
        }

        /// <summary>
        /// Connect to given IP and port.
        /// </summary>
        /// <param name="ip">IP to connect to or null to use localhost.</param>
        /// <param name="port">Port to connect to.</param>
        /// <param name="eventsListener">Object to handle socket events.</param>
        public SocketClient(IEventsListener eventsListener)
        {
            try
            {
                string ip = this.config.sc.Broker.Host;
                int port = this.config.sc.Broker.Port;

                // store events listener
                _eventsListener = eventsListener;

                // get session id
                this.SocketId = Interlocked.Increment(ref _nextSocketId);

                // Establish the remote endpoint for the socket.  
                // The name of the
                // remote device is "host.contoso.com".  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(ip);
                if(ipHostInfo.AddressList.Length == 0)
                {
                    throw new Exception($"Unknow host / ip : {ip}");
                }
                IPAddress ipAddress = ipHostInfo.AddressList[ipHostInfo.AddressList.Length - 1];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.  
                this.Client = new Socket(
                    ipAddress.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                /*
                // Connect to the remote endpoint.  
                this.Client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), 
                    this.Client);
                */
                this.Client.Connect(ip, port);
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            // invoke event
            _eventsListener.OnNewConnection(this);
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        /// <summary>
        /// Create the socket from existing connected socket.
        /// </summary>
        /// <param name="socket">TCP client to wrap.</param>
        /// <param name="eventsListener">Object to handle socket events.</param>
        public SocketClient(Socket handler, IEventsListener eventsListener)
        {
            try
            {
                // store events listener
                _eventsListener = eventsListener;

                this.Client = handler;

                // init socket
                SocketId = Interlocked.Increment(ref _nextSocketId);
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            // invoke event
            _eventsListener.OnNewConnection(this);
        }

        /// <summary>
        /// Socket destructor.
        /// </summary>
        ~SocketClient()
        {
            Close();
        }

        public void StartReadingMessages()
        {
            if (this.Client != null)
            {
                this.Reading = true;
                this.BeginReceive();
            }
        }

        private void BeginReceive(StateObject state = null)
        {
            if (this.Reading)
            {
                // Create the state object.
                if (state == null)
                {
                    state = new StateObject();
                    state.workSocket = this.Client;
                }
                this.Client.BeginReceive(
                    state.buffer,
                    0,
                    state.frameLength,
                    SocketFlags.None,
                    new AsyncCallback(ReadCallback),
                    state);
            }
        }

        public void StopReadingMessages()
        {
            if (this.Client != null)
            {
                this.Reading = false;
            }
        }

        /// <summary>
        /// Close the connection.
        /// </summary>
        public virtual void Close()
        {
            // close stream and socket
            // call event earlier so disconnect event will always be able to make use of the tcpclient before its disposed
            // invoke event
            if (!_wasClosedEventCalled)
            {
                _eventsListener.OnConnectionClosed(this);
                _wasClosedEventCalled = true;
            }

            try
            {
                if (this.Client != null && this.Client.Connected)
                {
                    this.Client.Shutdown(SocketShutdown.Both);
                    this.Client.Close();
                }
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        #endregion

        #region Send Methods


        public void SendMessage(byte[] data)
        {
            if (this.Client != null)
            {
                // sanity check
                if (data.Length == 0)
                {
                    throw new Exception("Cannot encode empty buffer!");
                }

                // to encode message size
                byte[] msgBuffer = new byte[this.FrameLengthByteNumber + data.Length];
                /*
                // could optionally call BitConverter.GetBytes(data.length);
                msgBuffer[0] = (byte)data.Length;
                msgBuffer[1] = (byte)(data.Length >> 8);
                msgBuffer[2] = (byte)(data.Length >> 16);
                msgBuffer[3] = (byte)(data.Length >> 24);*/

                this.FrameLength(data.Length, ref msgBuffer);

                // merge and send size + data
                Buffer.BlockCopy(data, 0, msgBuffer, 4, data.Length);

                // Begin sending the data to the remote device.
                this.Client.BeginSend(
                    msgBuffer,
                    0,
                    msgBuffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(SendCallback),
                    null);

                // invoke events
                _eventsListener.OnMessageSend(this, msgBuffer);
                _eventsListener.OnDataSend(this, data);
            }
        }


        /// <summary>
        /// Convert bytes array to int.
        /// </summary>
        private void FrameLength(int length, ref byte[] buff)
        {
            byte byteIdx = 0;
            for (int i = 0; i < this.FrameLengthByteNumber; i++)
            {
                buff[byteIdx] = (byte)(length >> (8 * byteIdx));
                byteIdx++;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Complete sending the data to the remote device.
                int bytesSent = this.Client.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        #endregion

        #region Read Methods

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                if (state.State == StateObject.States.Data)
                {

                    // invoke events
                    _eventsListener.OnDataRead(this, state.buffer);
                    _eventsListener.OnMessageRead(this, state.buffer);

                    // Init for next frame
                    state.ModifyState();
                }
                else
                {
                    _eventsListener.OnDataRead(this, state.buffer);

                    int frameLength = this.FrameLength(state.buffer);


                    // Next frame
                    state.ModifyState(frameLength);
                }
            }

            BeginReceive(state);

        }

        /// <summary>
        /// Convert bytes array to int.
        /// </summary>
        private int FrameLength(byte[] arr)
        {
            // convert the size we read into int
            // could optionally call BitConverter.ToInt32(sizeinfo, 0);
            if (arr == null) { return 0; }
            int ret = 0;
            byte byteIdx = 0;
            foreach (byte b in arr)
            {
                ret |= (((int)arr[byteIdx]) << (8 * byteIdx));
                byteIdx++;
            }
            return ret;
        }

        #endregion


    }
}
