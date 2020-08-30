using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nomad_V2
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Contains("CO=1"))
                    DataWrangler.OverrideCollisionProtection = true;

                if (args.Any(a => a.StartsWith("S=")))
                    DataWrangler.Server = args.First(a => a.StartsWith("S=")).Substring("S=".Length);

                if (args.Any(a => a == "-verbose" || a == "-v"))
                    DataWrangler.Verbose = true;


                if (args.Any(a => a == "-basic" || a == "-b"))
                    DataWrangler.Basic = true;

                if (args.Any(a => a.StartsWith("MS=")))
                   MotorController.MaxSpeed = Convert.ToDouble(args.First(a => a.StartsWith("MS=")).Substring("MS=".Length));


                Console.WriteLine("OverrideCollisionProtection:" + DataWrangler.OverrideCollisionProtection);
                Console.WriteLine("Server:" + DataWrangler.Server);

                Comunicator.ServerDataRecived += Comunicator_ServerDataRecived;
                Comunicator.Connect(DataWrangler.Server);

                // "sonar.py", "steering.py", "compass.py", "gps.py", "rotation.py", 

                DataWrangler.clients.Add("sonar.py");
                DataWrangler.clients.Add("steering.py");


                SQLLogger.Init();

                MotorController.Init();

                if (!DataWrangler.Basic)
                {
                    DataWrangler.clients.Add("compass.py");
                    DataWrangler.clients.Add("gps.py");
                    DataWrangler.clients.Add("rotation.py");

                    GPSController.Init();
                    Compass.Init();
                    WheelRotation.Init();
                }

                SensorServer.Init();

                CollisionProtection.Init();
                CollisionProtection.ObsticleDangerouslyClose += CollisionProtection_ObsticleDangerouslyClose ;
                WheelRotation.Init();

                foreach (var client in DataWrangler.clients)
                {
                    FunctionHelper.StartClientProcesses(client);
                    Thread.Sleep(300);
                }

                Thread.Sleep(1000);

                Console.Read();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FINAL CATCH REACHED!!! " + ex);
                MotorController.Kill();
            }
        }


        private static void SonarController_DistacesUpdated(object sender, int[] e)
        {

        }


        private static void Comunicator_ServerDataRecived(object sender, string e)
        {
            try
            {

                if (string.IsNullOrEmpty(e))
                    return;

                var command = e.Split(';');

                switch (command[0])
                {
                    case "Direct":
                        try
                        {
                            if (DataWrangler.OPMode == OPMode.Direct)
                            {
                                MotorController.Steer(Convert.ToInt32(command[1]));


                                if ((Convert.ToInt32(command[2]) > 0) ||
                                    (Convert.ToInt32(command[2]) < 0))
                                {
                                    MotorController.Exelarate(Convert.ToInt32(command[2]));

                                }
                                else if (Convert.ToInt32(command[2]) == 0)
                                {
                                    MotorController.Exelarate(Convert.ToInt32(command[2]));
                                    MotorController.MotorBlocked = false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("RECIVED MSG:" + e);
                            Console.WriteLine("ERROR WHILE PARSING DIRECT CONTROLL MSG:" + ex);
                        }
                        break;


                    case "Target":
                        if (DataWrangler.OPMode == OPMode.Automatic)
                        {
                            Driver.Target = new GPSPoint(Convert.ToDouble(command[1], CultureInfo.InvariantCulture), Convert.ToDouble(command[2], CultureInfo.InvariantCulture));
                            Driver.DriveToTarget();
                        }
                        break;

                    case "Explore":

                        Console.WriteLine("Recived Exploring cmd");

                        if (command[1] == "1")
                        {
                            Console.WriteLine("Start exploring");
                            if (DataWrangler.OPMode == OPMode.Automatic)
                            {
                                Console.WriteLine("OPMode:" + DataWrangler.OPMode.ToString());
                                Driver.Explore();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Stop exploring");
                            Driver.Stop();
                        }
                        break;

                    case "Mode":
                        Driver.Stop();
                        DataWrangler.OPMode = (OPMode)Enum.ToObject(typeof(OPMode), Convert.ToInt32(command[1]));

                        if (DataWrangler.OPMode == OPMode.Direct)
                        {

                        }
                        else
                        {

                        }

                        break;

                    case "Setup":
                        Console.WriteLine(e);

                        switch (command[1])
                        {
                            case "Sensor":
                                switch (command[2])
                                {
                                    case "Sonar":

                                        switch (command[3])
                                        {
                                            case "Enabled":

                                                CollisionProtection.SetupSensor(Convert.ToInt32(command[4]));


                                                break;
                                        }

                                        break;
                                }

                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR WHILE PROCESSING SERVER INPUT:"+ex.Message);
                Console.WriteLine("INPUT:" + e);
            }
        }


        private static void CollisionProtection_ObsticleDangerouslyClose(object sender, int e)
        {
            if (!DataWrangler.OverrideCollisionProtection)
            {
                MotorController.EmergencyBreak("Collision protection:" + e);
            }
        }
    }
}
