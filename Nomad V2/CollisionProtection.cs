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
        public static List<Sensor> Sensors = new List<Sensor>();

        public static List<int> IgnoredSensors = new List<int>();

        public static bool Verbose = false;

        public static event EventHandler<int> ObsticleDangerouslyClose;

        public static int CriticalDistance { get; set; } = 80;

        public static int CriticalFloorDistance { get; set; } = 20;


        public static void Init()
        {
            Console.WriteLine("CollisionProtection init...");
            SensorServer.SonarDataRecived += SensorServer_SonarDataRecived;

            Sensors.Add(new Sensor(0));
            Sensors.Add(new Sensor(1));
            Sensors.Add(new Sensor(2));
            Sensors.Add(new Sensor(3));
            Sensors.Add(new Sensor(4));

        }

        private static void SensorServer_SonarDataRecived(object sender, string data)
        {

            if (data.StartsWith("<SOT>"))
                data = data.Substring("<SOT>".Length);

            if (data.Contains("<EOT>"))
                data = data.Substring(0, data.IndexOf("<EOT>"));

            if (Verbose)
                Console.WriteLine(data);

            var values = data.Split(',');

            for (int i = 0; i < values.Length; i++)
            {
                Sensors[i].Distance = Math.Round(Convert.ToDouble(values[i], CultureInfo.InvariantCulture), 0);


                //Check if obsticle is close / floor to far away
                if (i <= 3)
                {
                    //Debug
                    if(i==0)
                    if (Sensors[i].Distance <= CriticalDistance)
                        ObsticleDangerouslyClose?.Invoke(null, i);
                }
                else
                {
                    if (Sensors[i].Distance >= CriticalFloorDistance)
                        ObsticleDangerouslyClose?.Invoke(null, i);
                }
            }
        }

        public static string GetDistancesAsString()
        {
            string result = "";
            foreach (var sensor in Sensors)
            {
                result += sensor.Distance + ",";
            }
            result = result.Trim(',');

            return result;
        }

        internal static void SetupSensor(int SensorID)
        {
            Sensors[SensorID].Enabled = !Sensors[SensorID].Enabled;

            Console.WriteLine(SensorID + ";" + Sensors[SensorID].Enabled);

            Comunicator.SendToServer("Setup;Sensor;Sonar;Enabled;" + SensorID + ";" + Convert.ToInt32(Sensors[SensorID].Enabled));
        }
    }


    public class Sensor
    {
        public int ID { get; set; }

        private double distance;
        public double Distance
        {
            get
            {
                if (Enabled)
                    return distance;
                else
                    return 1001;
            }

            set 
            {
                distance = value;
            }
        }
        public bool Enabled { get; set; } = true;


        public Sensor(int id)
        {
            ID = id;
        }
    }
}
