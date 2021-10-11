using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoBUS.Db
{
    class DictionaryDb<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private string path;
        public DictionaryDb(string path)
        {
            this.path = path;
        }

        public new void Add(TKey key, TValue value)
        {
            try
            {
                this.Write(key, value);
                base.Add(key, value);
            }
            catch { }
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            try
            {

                if(base.TryAdd(key, value))
                {
                    this.Write(key, value);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public new void Clear()
        {
            try
            {
                foreach(string path in Directory.GetFiles(this.path))
                {
                    File.Delete(path);
                }

                base.Clear();
            }
            catch { }
        }

        public new void Remove(TKey key)
        {
            try
            {
                string path = Path.Combine(this.path, key.ToString());
                if(File.Exists(path))
                {
                    File.Delete(path);
                }

                base.Remove(key);
            }
            catch { }
        }


        public new virtual TValue this[TKey key]
        {
            get
            {
                if(!base.ContainsKey(key))
                {
                    base[key] = this.Read(key);
                }
                return base[key];
            }
            set
            {
                this.Write(key, value);
                base[key] = value;
            }
        }

        private void Write(TKey key, TValue value)
        {
            try
            {
                string jsonvalue = JsonSerializer.Serialize<TValue>(value,
                    new JsonSerializerOptions()
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });

                File.WriteAllText(Path.Combine(this.path, key.ToString()), jsonvalue);
            }
            catch { }
        }

        public void Save()
        {
            foreach(TKey key in base.Keys)
            {
                TValue value = base[key];
                this.Write(key, value);
            }
        }

        private TValue Read(TKey key)
        {
            TValue value = default(TValue);
            try
            {
                string jsonvalue = File.ReadAllText(Path.Combine(this.path, key.ToString()));
                value = JsonSerializer.Deserialize<TValue>(jsonvalue);
            }
            catch { }

            return value;
        }

        public void Open()
        {
            try
            {
                foreach (string path in Directory.GetFiles(this.path))
                {
                    string _key = Path.GetFileName(path);
                    TKey key = (TKey)Convert.ChangeType(_key, typeof(TKey));
                    base.Add(key, this.Read(key));
                }
            }
            catch { }
        }
    }
}
