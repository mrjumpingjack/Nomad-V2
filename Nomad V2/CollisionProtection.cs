using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class CollisionProtection
    {
        public static double[] Distances = new double[] { -1, -1, -1, -1, -1 };

        public static bool Verbose = false;

        public static event EventHandler ObsticleDangerouslyClose;

        public static int CriticalDistance { get; set; } = 20;

        public static void Init()
        {
            Console.WriteLine("CollisionProtection init...");
            SensorServer.SonarDataRecived += SensorServer_SonarDataRecived;
        }

        private static void SensorServer_SonarDataRecived(object sender, string data)
        {

            if (data.StartsWith("<SOT>"))
                data = data.Substring("<SOT>".Length);

            if (data.Contains("<EOT>"))
                data = data.Substring(0, data.IndexOf("<EOT>"));

            if(Verbose)
                Console.WriteLine(data);

            var values = data.Split(',');

            for (int i = 0; i < values.Length; i++)
            {
                Distances[i] = Math.Round(Convert.ToDouble(values[i], CultureInfo.InvariantCulture),0);
            }

            if (Distances.Any(d => d <= CriticalDistance))
                ObsticleDangerouslyClose?.Invoke(null, null);

        }

        public static double[] GetDistances()
        {
            return Distances;
        }

        public static string GetDistancesAsString()
        {
            return string.Join(",", Distances);
        }

    }
}
