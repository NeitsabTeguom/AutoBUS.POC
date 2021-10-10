using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBUS
{
    public partial class ConfigManager
    {
        #region Common config

        public class ServiceConfig
        {
            public Broker Broker { get; set; } = new Broker();

            public ServiceConfig()
            {
            }
            public ServiceConfig(AutoBUS.Broker.BrokerTypes brokerType)
            {
                switch(brokerType)
                {
                    case AutoBUS.Broker.BrokerTypes.Federator:
                        {
                            this.Broker.Worker = null;
                            break;
                        }
                    case AutoBUS.Broker.BrokerTypes.Worker:
                        {
                            this.Broker.Federator = null;
                            break;
                        }
                }
            }
        }

        public class Broker
        {
            public Federator Federator { get; set; } = new Federator();
            public Worker Worker { get; set; } = new Worker();

            public int Port { get; set; } = 11000;
            public double CheckInterval { get; set; } = 1000;
        }

        #endregion Common config

        #region Main service config

        public class Federator
        {
            public string ListenHost { get; set; } = "localhost";
            public int ListenBacklog { get; set; } = 100;

            private Dictionary<string, Login> _Logins = new Dictionary<string, Login>();
            public Dictionary<string, Login> Logins 
            {
                get
                {
                    if(this._Logins.Count == 0)
                    {
                        this._Logins = new Dictionary<string, Login>() { { "Worker", new Login() { Password = "w0rkH@rd!", Privileges = new Privileges() } } };
                    }
                    return this._Logins;
                }
                set
                {
                    this._Logins = value;
                }
            }
        }

        public class Login
        {
            public string Password { get; set; }

            public Privileges Privileges { get; set; } = new Privileges();

        }

        public class Privileges
        {
            public bool TaskExecute { get; set; } = true;
        }

        #endregion Main service config

        #region Worker service config

        public class Worker
        {
            public string Host { get; set; } = "localhost";
            public string User { get; set; } = "Worker";
            public string Password { get; set; } = "w0rkH@rd!";
        }

        #endregion Worker service config
    }
}
