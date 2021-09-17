using System;

namespace AutoBUS.Messages
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

            // Send the right version to the Worker if nécessary (Worker version better than Main)
            if (clientVersion > this.broker.BrokerVersion)
            {
                this.broker.ms.VersionCheck(SocketId);
            }

            SocketMiddleware.SocketInfos infos = this.broker.sm.GetSocketInfo(SocketId);
            infos.NegociateVersion = this.broker.BrokerVersion;
            this.broker.sm.SetSocketInfo(SocketId, infos);
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
        public void VersionCheck(long SocketId)
        {
            SocketMiddleware.SocketInfos infos = this.broker.sm.GetSocketInfo(SocketId);
            infos.NegociateVersion = this.broker.BrokerVersion;
            this.broker.sm.SetSocketInfo(SocketId, infos);

            byte[] buff = BitConverter.GetBytes((UInt16)this.broker.BrokerVersion);
            this.broker.Deliver(SocketId, buff);
        }
    }
}
