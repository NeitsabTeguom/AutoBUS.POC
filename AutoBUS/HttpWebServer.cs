using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace AutoBUS
{
    class HttpWebServer
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly Broker broker = new Broker();

        public HttpWebServer(IReadOnlyCollection<string> prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            // URI prefixes are required eg: "http://localhost:8080/test/"
            if (prefixes == null || prefixes.Count == 0)
            {
                throw new ArgumentException("URI prefixes are required");
            }

            foreach (var s in prefixes)
            {
                this.listener.Prefixes.Add(s);
            }

            this.listener.Start();
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Console.WriteLine("HttpServer running...");
                try
                {
                    while (this.listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            var hlc = c as HttpListenerContext;
                            try
                            {
                                if (hlc == null)
                                {
                                    return;
                                }

                                this.broker.Deliver(ref hlc);
                                hlc.Response.KeepAlive = true;
                            }
                            catch
                            {
                                // ignored
                            }
                            finally
                            {
                                // always close the stream
                                if (hlc != null)
                                {
                                    hlc.Response.OutputStream.Close();
                                }
                            }
                        },
                        this.listener.GetContext());
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }
            });
        }

        public void Stop()
        {
            this.listener.Stop();
            this.listener.Close();
        }
    }
}
