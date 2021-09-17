using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoBUS
{
    public partial class Config
    {
        public enum ServiceTypes { Main, Worker }

        public ServiceTypes serviceType { get; private set; }

        public string configFile { get; private set; }

        public ServiceConfigMain serviceConfigMain;

        public ServiceConfigWorker serviceConfigWorker;

        public Config(ServiceTypes serviceType)
        {
            this.serviceType = serviceType;
            this.LoadConfig();
        }

        private void LoadConfig()
        {
            this.configFile = Paths.ConfigFile;

            // Save config file with default values if not exists
            if(!File.Exists(this.configFile))
            {
                this.SaveConfig();
            }

            // Load config file
            if (File.Exists(this.configFile))
            {
                this.OpenConfig();
            }


        }

        private void SaveConfig()
        {
            ServiceConfig sc = this.GetConfig();

            string jsonConfig = JsonSerializer.Serialize(sc, this.GetConfigType(), new JsonSerializerOptions() { WriteIndented = true });

            File.WriteAllText(this.configFile, jsonConfig);
        }

        private void OpenConfig()
        {
            string jsonConfig = File.ReadAllText(this.configFile);

            object sc = JsonSerializer.Deserialize(jsonConfig, this.GetConfigType());

            this.SetConfig(sc);
        }

        private ServiceConfig GetConfig()
        {
            ServiceConfig sc = null;

            if (this.serviceType == ServiceTypes.Main)
            {
                if (this.serviceConfigMain == null)
                {
                    sc = new ServiceConfigMain();
                }
                else
                {
                    sc = this.serviceConfigMain;
                }
            }
            else if (this.serviceType == ServiceTypes.Worker)
            {
                if (this.serviceConfigWorker == null)
                {
                    sc = new ServiceConfigWorker();
                }
                else
                {
                    sc = this.serviceConfigWorker;
                }
            }

            return sc;
        }

        private Type GetConfigType()
        {
            Type t = null;

            if (this.serviceType == ServiceTypes.Main)
            {
                t = typeof(ServiceConfigMain);
            }
            else if (this.serviceType == ServiceTypes.Worker)
            {
                t = typeof(ServiceConfigWorker);
            }

            return t;
        }

        private void SetConfig(object sc)
        {
            if (this.serviceType == ServiceTypes.Main)
            {
                if (sc == null)
                {
                    sc = new ServiceConfigMain();
                }
                this.serviceConfigMain = (ServiceConfigMain)sc;
            }
            else if (this.serviceType == ServiceTypes.Worker)
            {
                if (sc == null)
                {
                    sc = new ServiceConfigWorker();
                }
                this.serviceConfigWorker = (ServiceConfigWorker)sc;
            }
        }
    }
}
