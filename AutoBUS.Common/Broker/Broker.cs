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

        public UInt16 BrokerVersion { get; private set; } = 1;

        public class Frame
        {
            private static class Utils
            {
                // UTF8 par defaut
                private static Encoding encoding = Encoding.UTF8;

                public static byte[] ConvertStringToBytes(string msg)
                {
                    return encoding.GetBytes(msg);
                }

                public static string ConvertBytesToString(byte[] msg)
                {
                    return encoding.GetString(msg);
                }
            }

            public class Header
            {
                // Required properties
                public string MessageName { get; set; }
                public string RequestId { get; set; }

                // Not required properties

                // Managing frames fragmentation

                // Frame index part for this request Id
                public uint? PartIndex { get; private set; }
                // Frames needed for this request Id
                public uint? PartLength { get; private set; }

                // Parameters

                public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
            }

            public Header header = new Header();

            public byte[] DataBytes { get; set; }
            private string _DataString;
            public string DataString
            {
                get
                {
                    if (this._DataString == null)
                    {
                        this._DataString = Utils.ConvertBytesToString(this.DataBytes);
                    }
                    return this._DataString;
                }
                set
                {
                    if (this.DataBytes == null)
                    {
                        this.DataBytes = Utils.ConvertStringToBytes(value);
                    }
                }
            }

            // Buff -> Frame
            public Frame(byte[] buff)
            {
                int cursor = 0;
                this.GetHeader(buff, ref cursor);
                this.GetData(buff, ref cursor);
            }

            // Frame -> Buff
            public Frame(string MessageName, string RequestId)
            {
                this.header.MessageName = MessageName;
                this.header.RequestId = RequestId;
            }

            #region Buff2Frame

            /// <summary>
            /// Get header in received message
            /// </summary>
            /// <param name="buff"></param>
            /// <returns></returns>
            private void GetHeader(byte[] buff, ref int cursor)
            {
                this.header.Parameters = new Dictionary<string, string>();

                Type t = this.header.GetType();

                cursor = 0;

                while (cursor < buff.Length)
                {
                    string name = "";
                    string value = "";

                    this.GetHeaderParam(buff, ref cursor, out name, out value);

                    // If the property exist in this class, then set it; otherwise put it into parameters dictionary
                    if (name != "Data" && name != "Parameters")
                    {
                        PropertyInfo pi = t.GetProperty(name);
                        if (pi != null)
                        {
                            pi.SetValue(this.header, Convert.ChangeType(value, pi.PropertyType));
                        }
                        else
                        {
                            this.header.Parameters.Add(name, value);
                        }
                    }
                    else
                    {
                        this.header.Parameters.Add(name, value);
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

            private void GetData(byte[] buff, ref int cursor)
            {
                this.DataBytes = new byte[buff.Length - cursor];
                Buffer.BlockCopy(buff, cursor, this.DataBytes, 0, this.DataBytes.Length);
            }

            #endregion Buff2Frame

            #region Frame2Buff
            public byte[] GetBuffer()
            {
                if (this.header != null)
                {
                    string header = "";

                    Type t = this.header.GetType();

                    foreach (PropertyInfo pi in t.GetProperties())
                    {
                        if (t.Name != "Parameters")
                        {
                            object value = pi.GetValue(this.header);
                            if (value != null)
                            {
                                header += pi.Name + ":" + value.ToString() + "\n";
                            }
                        }
                    }

                    foreach(KeyValuePair<string, string> parameter in this.header.Parameters)
                    {
                        // Parameter can't be a property of header
                        if(t.GetProperty(parameter.Key)==null)
                        {
                            header += parameter.Key + ":" + parameter.Value + "\n";
                        }
                    }

                    header += "\n";

                    byte[] frame = Utils.ConvertStringToBytes(header);
                    int frameLength = frame.Length;

                    // Adding data

                    if (this.DataBytes != null && this.DataBytes.Length > 0)
                    {
                        Array.Resize(ref frame, frame.Length + this.DataBytes.Length);
                        Buffer.BlockCopy(this.DataBytes, 0, frame, frameLength, this.DataBytes.Length);
                    }

                    return frame;
                }
                return null;
            }
            #endregion Frame2Buff

            /// <summary>
            /// Reading header parameter
            /// </summary>
            /// <param name="header"></param>
            /// <param name="paramName"></param>
            /// <returns></returns>
            public string ReadHeaderParam(string paramName)
            {
                if (this.header != null && this.header.Parameters != null && this.header.Parameters.ContainsKey(paramName))
                {
                    return this.header.Parameters[paramName];
                }
                return null;
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
            Frame frame = new Frame(buff);


            // Check MessageName
            if (frame.header.MessageName == null)
            {
                //this.ResponseError(SocketId, "", "missing MessageName.");
                return;
            }

            if(!msf.ContainsKey(frame.header.MessageName))
            {
                //this.ResponseError(SocketId, "", "unknow MessageName.");
                return;
            }
            MethodInfo mf = msf[frame.header.MessageName];

            // Check RequestId not null or empty
            if (frame.header.RequestId == null || frame.header.RequestId.Trim() == "")
            {
                //this.ResponseError(SocketId, "", "missing RequestId.");
                return;
            }

            try
            {
                mf.Invoke(this.mr, new object[] { SocketId, frame });
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
            string MessageName = stackTrace.GetFrame(1).GetMethod().Name;
            string RequestId = this.options.MainServerNumber.ToString() + "_" + Guid.NewGuid().ToString();

            Frame frame = new Frame(MessageName, RequestId);
            frame.DataBytes = buff;

            this.sm.Send(SocketId, frame.GetBuffer());
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
            public void VersionCheck(long SocketId, Broker.Frame frame)
            {
                UInt16 clientVersion = BitConverter.ToUInt16(frame.DataBytes);

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
