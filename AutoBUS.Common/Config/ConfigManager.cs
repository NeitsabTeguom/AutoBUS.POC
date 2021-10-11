using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoBUS
{
    public partial class ConfigManager
    {
        public string configFile { get; private set; }

        private AutoBUS.Broker.BrokerTypes brokerType;

        public ServiceConfig sc { get; set; }

        public ConfigManager(AutoBUS.Broker.BrokerTypes brokerType)
        {
            this.LoadConfig(brokerType);
        }

        private void LoadConfig(AutoBUS.Broker.BrokerTypes brokerType)
        {
            this.brokerType = brokerType;
            this.sc = new ServiceConfig();
            this.SetConfigType();

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

        public void SaveConfig()
        {
            this.SetConfig();
                
            string jsonConfig = JsonSerializer.Serialize<ServiceConfig>(this.sc, 
                new JsonSerializerOptions() {
                    WriteIndented = true, 
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
                });

            File.WriteAllText(this.configFile, jsonConfig);
        }

        private void OpenConfig()
        {
            string jsonConfig = File.ReadAllText(this.configFile);

            this.sc = JsonSerializer.Deserialize<ServiceConfig>(jsonConfig);

            this.SetConfig(this.sc);
        }

        private void SetConfig(ServiceConfig sc = null)
        {
            if (this.sc == null)
            {
                this.sc = sc != null ? sc : new ServiceConfig();
            }
            this.SetConfigType();
        }

        private void SetConfigType()
        {
            switch (this.brokerType)
            {
                case AutoBUS.Broker.BrokerTypes.Federator:
                    {
                        this.sc.Broker.Worker = null;
                        break;
                    }
                case AutoBUS.Broker.BrokerTypes.Worker:
                    {
                        this.sc.Broker.Federator = null;
                        break;
                    }
            }
        }
    }
}
