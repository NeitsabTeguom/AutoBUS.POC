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

        public class Header
        {
            public string MessageName { get; private set; }
            public string RequestId { get; private set; }

            public Dictionary<string, string> Parameters { get; private set; }

            public int cursor { get; private set; }

            public Header(byte[] buff)
            {
                this.cursor = this.GetHeader(buff);
            }

            /// <summary>
            /// Reading header parameter
            /// </summary>
            /// <param name="header"></param>
            /// <param name="paramName"></param>
            /// <returns></returns>
            public string ReadHeaderParam(string paramName)
            {
                if (this.Parameters != null && this.Parameters.ContainsKey(paramName))
                {
                    return this.Parameters[paramName];
                }
                return null;
            }

            /// <summary>
            /// Get header in received message
            /// </summary>
            /// <param name="buff"></param>
            /// <returns></returns>
            private int GetHeader(byte[] buff)
            {
                this.Parameters = new Dictionary<string, string>();

                Type t = this.GetType();

                int cursor = 0;

                while (cursor < buff.Length)
                {
                    string name = "";
                    string value = "";

                    this.GetHeaderParam(buff, ref cursor, out name, out value);

                    // If the property exist in this class, then set it; otherwise put it into parameters dictionary
                    PropertyInfo pi = t.GetProperty(name);
                    if(pi != null)
                    {
                        pi.SetValue(this, Convert.ChangeType(value, pi.PropertyType));
                    }
                    else
                    {
                        this.Parameters.Add(name, value);
                    }

                    // End of buffer
                    if (cursor < buff.Length)
                    {
                        char c = Convert.ToChar(buff[cursor]);
                        if (c == '\n')
                        {
                            cursor++;
                            break;
                        }
                    }
                }

                return cursor;
            }

            /// <summary>
            /// Get param in message (header)
            /// </summary>
            /// <param name="buff"></param>
            /// <param name="cursor"></param>
            /// <param name="name"></param>
            /// <param name="value"></param>
            private void GetHeaderParam(byte[] buff, ref int cursor, out string name, out string value)
            {
                name = "";
                value = "";
                bool readingName = true;
                for (int i = cursor; i <= buff.Length; i++)
                {
                    char c = Convert.ToChar(buff[i]);
                    cursor = i + 1;
                    if (c == '\n')
                        break;

                    // Reading name to ':'
                    if (c == ':')
                    {
                        readingName = false;
                        continue;
                    }

                    if (readingName)
                    {
                        name += c;
                    }
                    else
                    {
                        value += c;
                    }
                }

                name = name.Trim();
                value = value.Trim();
            }
        }

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

            Header header = new Header(buff);
            int bodyBegin = header.cursor;


            // Check MessageName
            if (header.MessageName == null)
            {
                //this.ResponseError(SocketId, "", "missing MessageName.");
                return;
            }

            if(!msf.ContainsKey(header.MessageName))
            {
                //this.ResponseError(SocketId, "", "unknow MessageName.");
                return;
            }
            MethodInfo mf = msf[header.MessageName];

            // Check RequestId not null or empty
            if (header.RequestId == null || header.RequestId.Trim() == "")
            {
                //this.ResponseError(SocketId, "", "missing RequestId.");
                return;
            }

            try
            {
                byte[] data = new byte[buff.Length - bodyBegin];
                Buffer.BlockCopy(buff, bodyBegin, data, 0, data.Length);

                mf.Invoke(this.mr, new object[] { SocketId, header, data });
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


            byte[] data = this.ConvertStringToBytes($"MessageName:{MsgName}\nRequestId:{RequestId}\n\n");
            int dataLength = data.Length;


            Array.Resize(ref data, data.Length + buff.Length);
            Buffer.BlockCopy(buff, 0, data, dataLength, buff.Length);

            this.sm.Send(SocketId, data);
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
            public void VersionCheck(long SocketId, Broker.Header header, byte[] data)
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
