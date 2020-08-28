using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class Compass
    {
        public static double Heading = -1;


        public static void Init()
        {
            SensorServer.CompassDataRecived += SensorServer_CompassDataRecived;
        }

        private static void SensorServer_CompassDataRecived(object sender, string e)
        {
            Heading = Convert.ToDouble(e, CultureInfo.InvariantCulture);
        }
    }
}
