using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBUS
{
    public partial class Config
    {
        #region Common config

        public class ServiceConfig
        {
            public Broker Broker { get; set; } = new Broker();
        }

        public class Broker
        {
            public int Port { get; set; } = 11000;
        }

        #endregion Common config

        #region Main service config

        public class ServiceConfigMain : AutoBUS.Config.ServiceConfig
        {
        }

        #endregion Main service config

        #region Worker service config

        public class ServiceConfigWorker : AutoBUS.Config.ServiceConfig
        {
            public new Worker_Broker Broker { get; set; } = new Worker_Broker();
        }

        public class Worker_Broker : AutoBUS.Config.Broker
        {
            public string Host { get; set; } = "localhost";
        }

        #endregion Worker service config
    }
}
