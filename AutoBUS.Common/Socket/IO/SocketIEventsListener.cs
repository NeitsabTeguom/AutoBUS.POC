namespace AutoBUS.Sockets
{
    /// <summary>
    /// How to handle exceptions when raised.
    /// </summary>
    public enum ExceptionHandlerResponse
    {
        /// <summary>
        /// Silence the exception.
        /// </summary>
        Silence,

        /// <summary>
        /// Rethrow exception.
        /// </summary>
        Rethrow,

        /// <summary>
        /// Close socket without throwing exception.
        /// </summary>
        CloseSocket,
    }

    /// <summary>
    /// Class to handle different events from sockets.
    /// </summary>
    public interface IEventsListener
    {
        /// <summary>
        /// Called when data is sent.
        /// </summary>
        void OnDataSend(Socket socket, byte[] data);

        /// <summary>
        /// Called when data is read.
        /// </summary>
        void OnDataRead(Socket socket, byte[] data);

        /// <summary>
        /// Called when a whole framed message is sent.
        /// </summary>
        void OnMessageSend(Socket socket, byte[] data);

        /// <summary>
        /// Called when a whole framed message is read.
        /// </summary>
        void OnMessageRead(Socket socket, byte[] data);

        /// <summary>
        /// Called when a new connection is created.
        /// </summary>
        void OnNewConnection(Socket socket);

        /// <summary>
        /// Called when a connection is closed.
        /// </summary>
        void OnConnectionClosed(Socket socket);

        /// <summary>
        /// Called on exceptions.
        /// </summary>
        /// <returns>How to handle the exception.</returns>
        ExceptionHandlerResponse OnException(Socket socket, System.Exception exception);
    }
}