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
    }
}
