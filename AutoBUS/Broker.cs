using EzSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace AutoBUS
{
    // Message broker
    public class Broker
    {
        private SocketMiddleware sm;

        // Messages methods
        private Dictionary<string, MethodInfo> msf = new Dictionary<string, MethodInfo>();
        // Messages class instance, where to call functions
        private Messages.Receive mr;
        private Messages.Receive m;

        // UTF8 par defaut
        private Encoding encoding = Encoding.UTF8;

        private const UInt16 BrokerVersion = 1;

        public Broker(SocketMiddleware sm)
        {
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
        }

        /// <summary>
        /// Message Deliver
        /// </summary>
        /// <param name="hlc">HttpListenerContext Request / Response</param>
        public void Deliver(long SocketId, byte[] buff)
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
                this.ResponseBadRequest(SocketId, "", "missing MessageName.");
                return;
            }

            if(!msf.ContainsKey(MessageName))
            {
                this.ResponseBadRequest(SocketId, "", "unknow MessageName.");
                return;
            }
            MethodInfo mf = msf[MessageName];

            // Check RequestId not null or empty

            if (RequestId == null || RequestId.Trim() == "")
            {
                this.ResponseBadRequest(SocketId, "", "missing RequestId.");
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


        private int GetHeader(
            byte[] buff, 
            out string MessageName, 
            out string RequestId
            )
        {
            int cursor = 0;
            // 1 Message Name \n
            this.GetHeaderParam(buff, ref cursor, out MessageName);
            // 2 Request id \n
            this.GetHeaderParam(buff, ref cursor, out RequestId);

            return cursor;
        }

        public void GetHeaderParam(byte[] buff, ref int cursor, out string value)
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

        /// <summary>
        /// Response : Bad request
        /// </summary>
        /// <param name="hlc">HttpListenerContext Request / Response</param>
        /// <param name="description">Status description</param>
        public void ResponseBadRequest(long SocketId, string RequestId, string description)
        {
            this.sm.Send(SocketId, this.ReponseMessage(RequestId, 400, ("Bad request " + description).Trim()));
        }

        private byte[] ReponseMessage(string RequestId, UInt16 statusCode, string statusDescription = "")
        {
            return this.ConvertStringToBytes($"Response\n{RequestId}\n{statusCode}\n{statusDescription}".Trim() + "\n");
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
        public class Receive1
        {
            private readonly Broker broker;
            public Receive1(Broker broker)
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

                string VER = broker.ConvertBytesToString(data);

                if (VER == null)
                {
                    this.broker.ResponseBadRequest(SocketId, RequestId, "missing version in body.");
                    return;
                }


            }
        }
        public class Send1
        {
            private readonly Broker broker;
            public Send1(Broker broker)
            {
                this.broker = broker;
                Console.WriteLine("Messages ready to be sent...");
            }

            /// <summary>
            /// Check version
            /// </summary>
            /// <param name="SocketId"></param>
            /// <param name="data"></param>
            public void VersionCheck(long SocketId, string RequestId, byte[] data)
            {

                string VER = broker.ConvertBytesToString(data);

                if (VER == null)
                {
                    this.broker.ResponseBadRequest(SocketId, RequestId, "missing version in body.");
                    return;
                }


            }
        }
    }
}
