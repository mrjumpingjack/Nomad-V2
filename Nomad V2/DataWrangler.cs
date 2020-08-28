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
        public static Dictionary<string, Process> StartedClients { get; set; } = new Dictionary<string, Process>();

        public static List<string> clients = new List<string>() { "sonar.py", "steering.py", "compass.py", "gps.py", "rotation.py", };
    }
}
