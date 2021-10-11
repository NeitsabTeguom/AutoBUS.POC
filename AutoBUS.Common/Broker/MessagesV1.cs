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
                        string workerID = null;

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
                                    this.messages.Logged = true;

                                    // Has worker ID / otherwise give it new for first access
                                    if(receivedFrame.header.Parameters.ContainsKey("WorkerID"))
                                    {
                                        Broker.WorkerInfos wi = this.broker.Workers[receivedFrame.header.Parameters["WorkerID"]]; // Force reading in DB
                                    }
                                    else
                                    {
                                        workerID = Guid.NewGuid().ToString();
                                        this.broker.Workers[workerID] = new Broker.WorkerInfos(); // Force writing in DB
                                    }
                                    
                                }
                            }
                        }

                        // Response
                        Broker.Frame frame = new Broker.Frame();
                        frame.header.Parameters.Add("Logged", this.messages.Logged.ToString());
                        if(workerID != null)
                        {
                            frame.header.Parameters.Add("WorkerID", workerID);
                        }
                        this.messages.Send(SocketId, frame);

                        break;
                    }
                case Broker.BrokerTypes.Worker:
                    {
                        if (receivedFrame.header.Parameters.ContainsKey("Logged"))
                        {
                            // Federator logging response
                            string logged = receivedFrame.header.Parameters["Logged"];
                            if(receivedFrame.header.Parameters.ContainsKey("WorkerID"))
                            {
                                string workerID = receivedFrame.header.Parameters["WorkerID"];
                                this.broker.configManager.sc.Broker.Worker.WorkerID = workerID;
                                this.broker.configManager.SaveConfig();
                            }
                            this.messages.Logged = bool.Parse(logged);
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
                if(this.broker.configManager.sc.Broker.Worker.WorkerID != null)
                {
                    frame.header.Parameters.Add("WorkerID", this.broker.configManager.sc.Broker.Worker.WorkerID);
                }
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