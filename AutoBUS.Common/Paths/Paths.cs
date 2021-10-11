using System.IO;
using System.Reflection;

namespace AutoBUS
{
    public class Paths
    {
        private static string _ExecutingAssemblyPath;
        public static string ExecutingAssemblyPath
        {
            get
            {
                if (_ExecutingAssemblyPath == null)
                {
                    _ExecutingAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }
                return _ExecutingAssemblyPath;
            }
        }

        public static string ConfigFile
        {
            get
            {
                return Path.Combine(ExecutingAssemblyPath, "config.json");
            }
        }

        public static string DbPath
        {
            get
            {
                string dbPath = Path.Combine(ExecutingAssemblyPath, "db");
                if(!Directory.Exists(dbPath))
                {
                    try
                    {
                        Directory.CreateDirectory(dbPath);
                    }
                    catch { }
                }
                return dbPath;
            }
        }

        public static string DbWorkerPath
        {
            get
            {
                string dbWorkerPath = Path.Combine(DbPath, "worker");
                if (!Directory.Exists(dbWorkerPath))
                {
                    try
                    {
                        Directory.CreateDirectory(dbWorkerPath);
                    }
                    catch { }
                }
                return dbWorkerPath;
            }
        }
    }
}
