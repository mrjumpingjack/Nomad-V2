using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public enum OPMode
    {
        Direct = 0,
        Automatic = 1
    };

    public static class DataWrangler
    {
        public static OPMode OPMode = OPMode.Direct;

        public static bool OverrideCollisionProtection { get; set; } = false;
        public static string Server { get; set; }

        /// <summary>
        /// List of running python clients; Key is Clientrole"
        /// </summary>
        public static Dictionary<string, Process> RunningClients { get; set; } = new Dictionary<string, Process>();
        public static bool Verbose { get; internal set; }
        public static bool Basic { get; internal set; }

        public static List<string> clients = new List<string>() {};
    }
}
