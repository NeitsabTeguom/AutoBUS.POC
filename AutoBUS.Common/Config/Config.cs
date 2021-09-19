using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoBUS
{
    public partial class Config<TConfig> where TConfig : class, new()
    {
        public string configFile { get; private set; }

        public TConfig sc { get; set; } = new TConfig();

        public Config()
        {
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
            this.SetConfig();

            string jsonConfig = JsonSerializer.Serialize<TConfig>(this.sc, new JsonSerializerOptions() { WriteIndented = true });

            File.WriteAllText(this.configFile, jsonConfig);
        }

        private void OpenConfig()
        {
            string jsonConfig = File.ReadAllText(this.configFile);

            this.sc = JsonSerializer.Deserialize<TConfig>(jsonConfig);

            this.SetConfig(this.sc);
        }

        private void SetConfig(TConfig sc = null)
        {
            if (this.sc == null)
            {
                if (sc == null)
                {
                    this.sc = new TConfig();
                }
            }
        }
    }
}
