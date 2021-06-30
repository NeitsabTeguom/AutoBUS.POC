using EzSockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace AutoBUS
{
    // Message broker
    public class Broker
    {
        public class Options
        {
            public int MainServerNumber { get; set; }
        }

        public Options options;

        private enum StatusCode
        {
            Ack = 1, // Message received and saved to ROM
            Error, // Execution in timeout or all others error, error number in message
            Success // Message executed with success
        }

        public SocketMiddleware sm;

        // Messages methods
        private Dictionary<string, MethodInfo> msf = new Dictionary<string, MethodInfo>();
        // Messages class instance, where to call functions
        public Messages.Receive mr;
        public Messages.Send ms;

        // UTF8 par defaut
        private Encoding encoding = Encoding.UTF8;

        public UInt16 BrokerVersion { get; private set; } = 1;

        public Broker(SocketMiddleware sm)
        {
            this.options = new Options();
            this.options.MainServerNumber = 1;

            this.sm = sm;

            // For more time response, using "reflexion" instead "switch case"
            // but we nned to load functions in Messages class before

            Type mType = (typeof(Messages.Receive));
            // Get the public methods.
            MethodInfo[] mis = mType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach(MethodInfo mi in mis)
            {
                msf.Add(mi.Name, mi);
            }
            Console.WriteLine("Broker running...");

            this.mr = new Messages.Receive(this);

            this.ms = new Messages.Send(this);
        }

        /// <summary>
        /// Incomming message
        /// </summary>
        /// <param name="SocketId"></param>
        /// <param name="buff"></param>
        public void TakeIn(long SocketId, byte[] buff)
        {
            string MessageName;
            string RequestId;

            int bodyBegin = this.GetHeader(
                buff,
                out MessageName,
                out RequestId
                );


            // Check MessageName

            if (MessageName == null)
            {
                //this.ResponseError(SocketId, "", "missing MessageName.");
                return;
            }

            if(!msf.ContainsKey(MessageName))
            {
                //this.ResponseError(SocketId, "", "unknow MessageName.");
                return;
            }
            MethodInfo mf = msf[MessageName];

            // Check RequestId not null or empty

            if (RequestId == null || RequestId.Trim() == "")
            {
                //this.ResponseError(SocketId, "", "missing RequestId.");
                return;
            }

            try
            {
                byte[] data = new byte[buff.Length - bodyBegin];
                Buffer.BlockCopy(buff, bodyBegin, data, 0, data.Length);

                mf.Invoke(this.mr, new object[] { SocketId, RequestId, data });
            }
            catch(Exception ex){this.Logger(ex);}
        }

        /// <summary>
        /// Outgoing message
        /// </summary>
        /// <param name="SocketId"></param>
        /// <param name="buff"></param>
        public void Deliver(long SocketId, byte[] buff)
        {
            StackTrace stackTrace = new StackTrace();
            string MsgName = stackTrace.GetFrame(1).GetMethod().Name;
            string RequestId = this.options.MainServerNumber.ToString() + "_" + Guid.NewGuid().ToString();


            byte[] data = this.ConvertStringToBytes($"{MsgName}\n{RequestId}\n");
            int dataLength = data.Length;


            Array.Resize(ref data, data.Length + buff.Length);
            Buffer.BlockCopy(buff, 0, data, dataLength, buff.Length);

            this.sm.Send(SocketId, data);
        }

        /// <summary>
        /// Get header in received message
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="MessageName"></param>
        /// <param name="RequestId"></param>
        /// <returns></returns>
        private int GetHeader(
            byte[] buff, 
            out string MessageName, 
            out string RequestId
            )
        {
            int cursor = 0;
            // 1 Message Name \n
            this.GetParam(buff, ref cursor, out MessageName);
            // 2 Request id \n
            this.GetParam(buff, ref cursor, out RequestId);

            return cursor;
        }

        /// <summary>
        /// Get param in message (header)
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="cursor"></param>
        /// <param name="value"></param>
        public void GetParam(byte[] buff, ref int cursor, out string value)
        {
            value = "";
            for (int i = cursor; i <= buff.Length; i++)
            {
                char c = Convert.ToChar(buff[i]);
                cursor = i + 1;
                if (c == '\n')
                    break;
                value += c;
            }
        }

        public byte[] ConvertStringToBytes(string msg)
        {
            return this.encoding.GetBytes(msg);
        }

        public string ConvertBytesToString(byte[] msg)
        {
            return this.encoding.GetString(msg);
        }

        public void Logger(Exception ex)
        {

        }

    }

    namespace Messages
    {
        public class Receive
        {
            private readonly Broker broker;
            public Receive(Broker broker)
            {
                this.broker = broker;
                Console.WriteLine("Messages waiting to be called...");
            }

            /// <summary>
            /// Check version
            /// </summary>
            /// <param name="SocketId"></param>
            /// <param name="data"></param>
            public void VersionCheck(long SocketId, string RequestId, byte[] data)
            {
                UInt16 clientVersion = BitConverter.ToUInt16(data);

                if(clientVersion > this.broker.BrokerVersion)
                {
                    this.broker.ms.VersionCheck(SocketId);
                }

                SocketMiddleware.UserData userData = this.broker.sm.GetSocketData(SocketId);
                userData.NegociateVersion = this.broker.BrokerVersion;
                this.broker.sm.SetSocketData(SocketId, userData);
            }
        }

        public class Send
        {
            private readonly Broker broker;
            public Send(Broker broker)
            {
                this.broker = broker;
                Console.WriteLine("Messages ready to be sent...");
            }

            /// <summary>
            /// Check version
            /// </summary>
            /// <param name="SocketId"></param>
            /// <param name="data"></param>
            public void VersionCheck(long SocketId)
            {
                SocketMiddleware.UserData userData = this.broker.sm.GetSocketData(SocketId);
                userData.NegociateVersion = this.broker.BrokerVersion;
                this.broker.sm.SetSocketData(SocketId, userData);

                byte[] buff = BitConverter.GetBytes((UInt16)this.broker.BrokerVersion);
                this.broker.Deliver(SocketId, buff);
            }
        }
    }


}
