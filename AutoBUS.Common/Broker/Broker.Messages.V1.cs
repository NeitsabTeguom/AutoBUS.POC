namespace AutoBUS.Messages.V1
{
    public static class Receive
    {
        /// <summary>
        /// Login
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="SocketId"></param>
        /// <param name="frame"></param>
        public static void Login(Broker broker, long SocketId, Broker.Frame frame)
        {
            broker.sm[]
        }
    }

    public static class Send
    {
        /// <summary>
        /// Login
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="SocketId"></param>
        public static void Login(Broker broker, long SocketId)
        {
        }
    }
}