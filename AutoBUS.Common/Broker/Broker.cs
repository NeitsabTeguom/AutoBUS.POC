using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoBUS
{
    // Message broker
    public class Broker
    {
        public enum BrokerTypes
        {
            Federator,
            Worker
        }

        public BrokerTypes brokerType { get; private set; }

        public ConfigManager configManager { get; private set; }

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

        public class WorkerInfos
        {
            public byte Processor { get; set; }
            public byte Memory { get; set; }
        }

        public Db.DictionaryDb<string, WorkerInfos> Workers;

        public UInt16 BrokerVersion { get; private set; } = 1;

        public class Frame
        {
            public bool Available
            {
                get
                {
                    return this.ParsingError == null;
                }
            }
            public Exception ParsingError { get; private set; } = null;
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
                private string _MessageName;
                public string MessageName
                {
                    get
                    {
                        return this._MessageName;
                    }
                    set
                    {
                        if (this._MessageName==null)
                        {
                            this._MessageName = value;
                        }
                    }
                }
                private string _RequestId;
                public string RequestId
                {
                    get
                    {
                        return this._RequestId;
                    }
                    set
                    {
                        if (this._RequestId == null)
                        {
                            this._RequestId = value;
                        }
                    }
                }

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
                try
                {
                    int cursor = 0;
                    this.GetHeader(buff, ref cursor);
                    // Check MessageName
                    if (this.header.MessageName == null)
                    {
                        throw new Exception("Frame : missing MessageName on header.");
                    }
                    // Check RequestId not null or empty
                    if (this.header.RequestId == null || this.header.RequestId.Trim() == "")
                    {
                        throw new Exception("Frame : missing RequestId on header.");
                    }

                    this.GetData(buff, ref cursor);
                }
                catch(Exception ex)
                {
                    this.ParsingError = ex;
                }
            }

            // Frame -> Buff
            public Frame()
            {
            }
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

                if (buff != null)
                {
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
                if (buff != null)
                {
                    this.DataBytes = new byte[buff.Length - cursor];
                    Buffer.BlockCopy(buff, cursor, this.DataBytes, 0, this.DataBytes.Length);
                }
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
                        if (pi.Name != "Parameters" && pi.Name != "header")
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

        public Broker(BrokerTypes brokerType)
        {
            this.options = new Options();
            this.options.MainServerNumber = 1;

            this.brokerType = brokerType;
            this.configManager = new ConfigManager(this.brokerType);
            this.Workers = brokerType == BrokerTypes.Federator ? new Db.DictionaryDb<string, WorkerInfos>(Paths.DbPath, "worker") : null;
            this.sm = new SocketMiddleware(this);

        }

        public void Start()
        {
            this.sm.Start();
        }

        public void Stop()
        {
            this.sm.Stop();
        }


        /// <summary>
        /// Incomming message
        /// </summary>
        /// <param name="SocketId"></param>
        /// <param name="buff"></param>
        public void TakeIn(Sockets.SocketClient socket, byte[] buff)
        {
            Frame frame = new Frame(buff);

            // If frame not correct, so close connection (then worker reconnect and resend better)
            if(!frame.Available)
            {
                //Sockets.SocketClient sc = this.sm.GetSocketClient(socket.SocketId);
                socket.Close();
                return;
            }

            // TODO : Ecrit le fichier message

            try
            {
                //SocketMiddleware.SocketInfos si = this.sm.GetSocketInfo(socket.SocketId);
                socket.Infos.messages.Receive(frame);
            }
            catch(Exception ex){this.Logger(ex);}
        }

        /// <summary>
        /// Outgoing message
        /// </summary>
        /// <param name="SocketId"></param>
        /// <param name="buff"></param>
        public void Deliver(long SocketId, Frame sendFrame)
        {
            if (sendFrame.header.MessageName == null)
            {
                StackTrace stackTrace = new StackTrace();
                string MessageName = stackTrace.GetFrame(1).GetMethod().Name;
                sendFrame.header.MessageName = MessageName; // If not set only
            }

            string RequestId = this.options.MainServerNumber.ToString() + "_" + Guid.NewGuid().ToString();
            sendFrame.header.RequestId = RequestId; // If not set only

            if(this.sm.Send(SocketId, sendFrame.GetBuffer()))
            {
                // TODO : Supprimer le fichier message
            }
        }

        public void Logger(Exception ex)
        {

        }

    }


}
