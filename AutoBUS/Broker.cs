using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace AutoBUS
{
    // Message broker
    class Broker
    {
        // Messages methods
        private Dictionary<string, MethodInfo> msf = new Dictionary<string, MethodInfo>();
        // Messages class instance, where to call functions
        private Messages ms;

        public Broker()
        {
            // For more time response, using "reflexion" instead "switch case"
            // but we nned to load functions in Messages class before

            Type mType = (typeof(Messages));
            // Get the public methods.
            MethodInfo[] mis = mType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach(MethodInfo mi in mis)
            {
                msf.Add(mi.Name, mi);
            }
            Console.WriteLine("Broker running...");

            this.ms = new Messages(this);
        }

        /// <summary>
        /// Message Deliver
        /// </summary>
        /// <param name="hlc">HttpListenerContext Request / Response</param>
        public void Deliver(ref HttpListenerContext hlc)
        {
            // MSGTYPE as function calling
            string MSGTYPE = hlc.Request.Headers["X-API-MSGTYPE"];

            if (MSGTYPE == null)
            {
                this.ResponseBadRequest(ref hlc, "missing X-API-MSGTYPE in header.");
                return;
            }

            if(!msf.ContainsKey(MSGTYPE))
            {
                this.ResponseBadRequest(ref hlc, "unknow X-API-MSGTYPE.");
                return;
            }
            MethodInfo mf = msf[MSGTYPE];

            try
            {
                mf.Invoke(this.ms, new object[] { hlc });
            }
            catch(Exception ex){this.Logger(ex);}
            /*
            if (!request.HasEntityBody)
            {
                Console.WriteLine("No client data was sent with the request.");
                return "KO";
            }

            if (request.ContentType != null)
            {
                Console.WriteLine("Client data content type {0}", request.ContentType);
            }
            Console.WriteLine("Client data content length {0}", request.ContentLength64);

            */

        }

        private string RequestBodyToString(ref HttpListenerContext hlc)
        {
            string result = null;

            try
            {
                result = (new StreamReader(hlc.Request.InputStream, hlc.Request.ContentEncoding)).ReadToEnd();
            }
            catch (Exception ex) { this.Logger(ex); }

            return result;
        }

        /// <summary>
        /// Response : Bad request
        /// </summary>
        /// <param name="hlc">HttpListenerContext Request / Response</param>
        /// <param name="description">Status description</param>
        public void ResponseBadRequest(ref HttpListenerContext hlc, string description)
        {
            hlc.Response.StatusCode = 400;
            hlc.Response.StatusDescription = ("Bad request " + description).Trim();
        }

        public void Logger(Exception ex)
        {

        }

        private class Messages
        {
            private readonly Broker broker;
            public Messages(Broker broker)
            {
                this.broker = broker;
                Console.WriteLine("Messages waiting to be called...");
            }

            /// <summary>
            /// Check client version
            /// </summary>
            /// <param name="hlc">HttpListenerContext Request / Response</param>
            public void VersionCheck(ref HttpListenerContext hlc)
            {
                // MSGTYPE as function calling
                string VER = hlc.Request.Headers["X-API-VER"];

                if (VER == null)
                {
                    this.broker.ResponseBadRequest(ref hlc, "missing X-API-VER in header.");
                    return;
                }
            }
        }
    }
}
