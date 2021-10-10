using System;

namespace AutoBUS.MessagesV1
{
    public class Receive
    {
        private Broker broker;
        private Messages messages;

        public Receive(Broker broker, Messages messages)
        {
            this.broker = broker;
            this.messages = messages;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="SocketId"></param>
        /// <param name="receivedFrame"></param>
        public void Login(long SocketId, Broker.Frame receivedFrame)
        {

        }
    }

    public class Send
    {
        private Broker broker;
        private Messages messages;

        public Send(Broker broker, Messages messages)
        {
            this.broker = broker;
            this.messages = messages;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="SocketId"></param>
        public void Login(long SocketId)
        {
            //this.messages.Send(SocketId, );
        }
    }
}