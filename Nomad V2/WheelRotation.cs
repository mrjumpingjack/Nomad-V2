using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class WheelRotation
    {
        public static int Rotations { get; set; } = 0;
        public static double SpeedinKMh { get; set; } = 0;

        public static void Init()
        {
            SensorServer.WheelDataRecived += SensorServer_WheelDataRecived;
        }

        private static void SensorServer_WheelDataRecived(object sender, string e)
        {
            Rotations = Convert.ToInt32(e);

            double UmfangInM = 2 * Math.PI * 0.05;
            double SpeedinMpSek = (UmfangInM * (Rotations));
            SpeedinKMh = (SpeedinMpSek / 1000) / (1f / 3600f);
        }
    }
}
