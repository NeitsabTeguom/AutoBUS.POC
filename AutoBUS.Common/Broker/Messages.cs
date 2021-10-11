using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoBUS
{
    public class Messages
    {
        public bool Available { get; private set; } = false;
        public bool Logged = false;

        private Broker broker;

        private Sockets.SocketClient socket;

        // Messages class instance, where to call functions
        public object mr;
        // Messages methods
        private Dictionary<string, MethodInfo> mrf;

        // Messages class instance, where to call functions
        public object ms;
        // Messages methods
        private Dictionary<string, MethodInfo> msf;

        public Messages(Broker broker, Sockets.SocketClient socket)
        {
            this.broker = broker;
            this.socket = socket;
        }

        public void Init(string BrokerVersion)
        {
            // For more time response, using "reflexion" instead "switch case"
            // but we nned to load functions in Messages class before

            #region Receive

            Type tR = Assembly.GetExecutingAssembly().GetType("AutoBUS.MessagesV" + BrokerVersion + ".Receive");

            if (tR != null)
            {
                this.mrf = new Dictionary<string, MethodInfo>();
                // Get the public methods.
                MethodInfo[] misR = tR.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (MethodInfo miR in misR)
                {
                    mrf.Add(miR.Name, miR);
                }

                // New instance
                this.mr = Activator.CreateInstance(tR, broker, this);
            }

            #endregion

            #region Send

            Type tS = Assembly.GetExecutingAssembly().GetType("AutoBUS.MessagesV" + BrokerVersion + ".Send");

            if (tS != null)
            {
                this.msf = new Dictionary<string, MethodInfo>();
                // Get the public methods.
                MethodInfo[] misS = tS.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (MethodInfo miS in misS)
                {
                    msf.Add(miS.Name, miS);
                }

                // New instance
                this.ms = Activator.CreateInstance(tS, broker, this);
            }

            #endregion
        }

        #region Receive

        private Dictionary<string, Task> tasks = new Dictionary<string, Task>();
        public void Receive(Broker.Frame receiveFrame)
        {
            if (this.Available)
            {
                string taskID = Guid.NewGuid().ToString();
                Task obTask = (Task)Task.Run(() => this.ReceiveExecute(taskID, socket.SocketId, receiveFrame));
                this.tasks.Add(taskID, obTask);
                //obTask.result;
            }
            else if(receiveFrame.header.MessageName == "VersionCheck")
            {
                this.ReceiveVersionCheck(socket.SocketId, receiveFrame);
            }
        }

        private void ReceiveExecute(string taskID, long SocketId, Broker.Frame receiveFrame)
        {
            if (this.mrf.ContainsKey(receiveFrame.header.MessageName))
            {
                MethodInfo mi = this.mrf[receiveFrame.header.MessageName];

                if (mi != null)
                {
                    Broker.Frame sendFrame = (Broker.Frame)mi.Invoke(this.mr, new object[] { SocketId, receiveFrame });
                    if (sendFrame != null)
                    {
                        this.tasks.Remove(taskID);
                        this.broker.Deliver(SocketId, sendFrame);
                    }
                }
            }
        }

        private void SendExecute(long SocketId, string MessageName, object[] Parameters)
        {
            if (this.msf.ContainsKey(MessageName))
            {
                MethodInfo mi = this.msf[MessageName];

                if (mi != null)
                {
                    Broker.Frame sendFrame = (Broker.Frame)mi.Invoke(this.ms, Parameters);
                    if (sendFrame != null)
                    {
                        this.broker.Deliver(SocketId, sendFrame);
                    }
                }
            }
        }

        #region Version Check

        /// <summary>
        /// Check version
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="SocketId"></param>
        /// <param name="receivedFrame"></param>
        private void ReceiveVersionCheck(long SocketId, Broker.Frame receivedFrame)
        {
            UInt16 clientVersion = BitConverter.ToUInt16(receivedFrame.DataBytes);

            // Send the right version to the Worker if nécessary (Worker version better than Main)
            //SocketMiddleware.SocketInfos infos = broker.sm.GetSocketInfo(SocketId);
            if (this.socket.Infos.NegociateVersion == null)
            {
                this.socket.Infos.NegociateVersion = clientVersion < broker.BrokerVersion ? clientVersion : broker.BrokerVersion;
                this.Available = true;
                this.Init(this.socket.Infos.NegociateVersion.ToString());
                this.socket.Infos.messages = this;
                //broker.sm.SetSocketInfo(SocketId, infos);
            }

            switch(broker.brokerType)
            {
                case Broker.BrokerTypes.Federator:
                    {
                        this.VersionCheck(SocketId);
                        break;
                    }
                case Broker.BrokerTypes.Worker:
                    {
                        // Logged in after version check
                        this.SendExecute(SocketId, "Login", new object[] { SocketId });
                        break;
                    }
            }

        }

        /// <summary>
        /// Check version
        /// </summary>
        /// <param name="SocketId"></param>
        public void VersionCheck(long SocketId)
        {
            //SocketMiddleware.SocketInfos infos = broker.sm.GetSocketInfo(SocketId);
            Broker.Frame frame = new Broker.Frame();
            frame.DataBytes = BitConverter.GetBytes(this.socket.Infos.NegociateVersion != null ? (UInt16)this.socket.Infos.NegociateVersion : (UInt16)this.broker.BrokerVersion);
            this.broker.Deliver(SocketId, frame);
        }

        #endregion

        #endregion

        #region Send
        public void Send(long SocketId, Broker.Frame sendFrame)
        {
            StackTrace stackTrace = new StackTrace();
            string MessageName = stackTrace.GetFrame(1).GetMethod().Name;

            sendFrame.header.MessageName = MessageName; // If not set only

            this.broker.Deliver(SocketId, sendFrame);
        }
        #endregion

    }
}