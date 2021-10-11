using System;
using System.Security.Cryptography;
using System.Text;

namespace AutoBUS.MessagesV1
{
    public class Receive
    {
        private Broker broker;
        private Messages messages;
        private Utils utils;

        public Receive(Broker broker, Messages messages)
        {
            this.broker = broker;
            this.messages = messages;
            this.utils = new Utils();

        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="SocketId"></param>
        /// <param name="receivedFrame"></param>
        public void Login(long SocketId, Broker.Frame receivedFrame)
        {
            switch (this.broker.brokerType)
            {
                case Broker.BrokerTypes.Federator:
                    {
                        this.messages.Logged = false;

                        // Frame must contains User and Passwork to login
                        if (receivedFrame.header.Parameters.ContainsKey("User")
                            &&
                            receivedFrame.header.Parameters.ContainsKey("Password"))
                        {
                            //string pwdHash = this.utils.MD5Hash(receivedFrame.header.Parameters["Password"]);

                            // If user exist only
                            string userWorker = receivedFrame.header.Parameters["User"];
                            if (this.broker.configManager.sc.Broker.Federator.Logins.ContainsKey(userWorker))
                            {
                                var login = this.broker.configManager.sc.Broker.Federator.Logins[userWorker];

                                string passworkWorker = receivedFrame.header.Parameters["Password"];
                                // Ckeck password
                                if (login.Password == passworkWorker)
                                {
                                    /*
                                    var si = broker.sm.GetSocketInfo(SocketId);
                                    si.messages.Logged = true;
                                    broker.sm.SetSocketInfo(SocketId, si);
                                    */
                                    this.messages.Logged = true;
                                }
                            }
                        }

                        // Response
                        Broker.Frame frame = new Broker.Frame();
                        frame.header.Parameters.Add("Logged", this.messages.Logged.ToString());
                        this.messages.Send(SocketId, frame);

                        break;
                    }
                case Broker.BrokerTypes.Worker:
                    {
                        if (receivedFrame.header.Parameters.ContainsKey("Logged"))
                        {
                            // Federator logging response
                            string logged = receivedFrame.header.Parameters["Logged"];
                            this.messages.Logged = bool.Parse(logged);
                            /*
                            var si = broker.sm.GetSocketInfo(SocketId);
                            si.messages.Logged = bool.Parse(logged);
                            broker.sm.SetSocketInfo(SocketId, si);
                            */
                        }
                        break;
                    }
            }
        }


    }

    public class Send
    {
        private Broker broker;
        private Messages messages;
        private Utils utils;

        public Send(Broker broker, Messages messages)
        {
            this.broker = broker;
            this.messages = messages;
            this.utils = new Utils();
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="SocketId"></param>
        public void Login(long SocketId)
        {
            if (this.broker.brokerType == Broker.BrokerTypes.Worker)
            {
                Broker.Frame frame = new Broker.Frame();
                frame.header.Parameters.Add("User", this.broker.configManager.sc.Broker.Worker.User);
                frame.header.Parameters.Add("Password", this.broker.configManager.sc.Broker.Worker.Password);
                this.messages.Send(SocketId, frame);
            }
        }
    }

    public class Utils
    {
        public string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text  
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it  
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits  
                //for each byte  
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }
    }
}