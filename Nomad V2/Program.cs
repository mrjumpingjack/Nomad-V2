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


                Console.WriteLine("OverrideCollisionProtection:" + DataWrangler.OverrideCollisionProtection);
                Console.WriteLine("Server:" + DataWrangler.Server);

                //Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
                //{
                //    var isCtrlC = e.SpecialKey == ConsoleSpecialKey.ControlC;
                //    var isCtrlBreak = e.SpecialKey == ConsoleSpecialKey.ControlBreak;

                //    // Prevent CTRL-C from terminating
                //    if (isCtrlC)
                //    {
                //        SensorServer.Cancel = true;
                //        MotorController.Kill();
                //        e.Cancel = true;
                //    }
                //};

                Comunicator.ServerDataRecived += Comunicator_ServerDataRecived;
                Comunicator.Connect(DataWrangler.Server);

                // SQLLogger.Init();

                MotorController.Init();
                GPSController.Init();
                SensorServer.Init();
                Compass.Init();
                CollisionProtection.Init();
                CollisionProtection.ObsticleDangerouslyClose += CollisionProtection_ObsticleDangerouslyClose;
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
                        Driver.Target = new GPSPoint(Convert.ToDouble(command[0], CultureInfo.InvariantCulture), Convert.ToDouble(command[1], CultureInfo.InvariantCulture));
                        Driver.DriveToTarget();
                    }
                    break;

                case "Explore":
                    if (DataWrangler.OPMode == OPMode.Automatic)
                    {
                        Driver.Explore();
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
            }
        }


        private static void CollisionProtection_ObsticleDangerouslyClose(object sender, EventArgs e)
        {
            if (!DataWrangler.OverrideCollisionProtection)
            {
                MotorController.EmergencyBreak();
            }
        }
    }
}
