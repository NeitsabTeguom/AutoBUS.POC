using System;

namespace AutoBUS.Sockets
{
    /// <summary>
    /// Class to handle different events from sockets.
    /// </summary>
    public class EventsListener : IEventsListener
    {
        /// <summary>
        /// Called when data is sent.
        /// </summary>
        public Action<SocketClient, byte[]> OnDataSendHandler;

        /// <summary>
        /// Trigger the OnDataSend event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        /// <param name="data">Data sent.</param>
        public virtual void OnDataSend(SocketClient socket, byte[] data)
        {
            OnDataSendHandler?.Invoke(socket, data);
        }

        /// <summary>
        /// Called when data is read.
        /// </summary>
        public Action<SocketClient, byte[]> OnDataReadHandler;

        /// <summary>
        /// Trigger the OnDataRead event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        /// <param name="data">Data read.</param>
        public virtual void OnDataRead(SocketClient socket, byte[] data)
        {
            OnDataReadHandler?.Invoke(socket, data);
        }

        /// <summary>
        /// Called when a whole framed message is sent.
        /// </summary>
        public Action<SocketClient, byte[]> OnMessageSendHandler;

        /// <summary>
        /// Trigger the OnMessageSend event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        /// <param name="data">Data sent.</param>
        public virtual void OnMessageSend(SocketClient socket, byte[] data)
        {
            OnMessageSendHandler?.Invoke(socket, data);
        }

        /// <summary>
        /// Called when a whole framed message is read.
        /// </summary>
        public Action<SocketClient, byte[]> OnMessageReadHandler;

        /// <summary>
        /// Trigger the OnMessageRead event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        /// <param name="data">Data read.</param>
        public virtual void OnMessageRead(SocketClient socket, byte[] data)
        {
            OnMessageReadHandler?.Invoke(socket, data);
        }

        /// <summary>
        /// Called when a new connection is created.
        /// </summary>
        public Action<SocketClient> OnNewConnectionHandler;

        /// <summary>
        /// Trigger the OnNewConnection event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        public virtual void OnNewConnection(SocketClient socket)
        {
            OnNewConnectionHandler?.Invoke(socket);
        }

        /// <summary>
        /// Called when a connection is closed.
        /// </summary>
        public Action<SocketClient> OnConnectionClosedHandler;

        /// <summary>
        /// Trigger the OnConnectionClosed event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        public virtual void OnConnectionClosed(SocketClient socket)
        {
            OnConnectionClosedHandler?.Invoke(socket);
        }

        /// <summary>
        /// Called on exceptions.
        /// </summary>
        /// <returns>How to handle the exception.</returns>
        public Func<SocketClient, Exception, ExceptionHandlerResponse> OnExceptionHandler;

        /// <summary>
        /// Trigger the OnException event.
        /// </summary>
        /// <param name="socket">Socket the event originated from.</param>
        /// <param name="exception">Exception that triggered the event.</param>
        public virtual ExceptionHandlerResponse OnException(SocketClient socket, Exception exception)
        {
            if (OnExceptionHandler == null) { return ExceptionHandlerResponse.CloseSocket; }
            return OnExceptionHandler(socket, exception);
        }
    }
}