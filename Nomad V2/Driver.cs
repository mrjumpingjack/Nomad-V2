using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Nomad_V2
{
    public static class Driver
    {
        public static bool UseStreets = false;

        //Bundesstraßen
        public static bool UseHighways = false;

        //Autobahnen
        public static bool UseFreeways = false;


        public static Thread DriveToThread = null;

        public static int[] SaftyDistances = new int[] { 50, 50, 50, 50, 10 };


        public static GPSPoint Target { get; set; } = null;
        public static bool Cancel { get;  set; }

        public static void Init()
        {
            GPSController.GPSNotAvailable += GPSController_GPSNotAvailable;
            CollisionProtection.ObsticleDangerouslyClose += CollisionProtection_ObsticleDangerouslyClose;
        }

        private static void GPSController_GPSNotAvailable(object sender, EventArgs e)
        {
            Stop();
        }

        private static void CollisionProtection_ObsticleDangerouslyClose(object sender, EventArgs e)
        {
            MotorController.EmergencyBreak();

        }

        public static void Stop()
        {
            try
            {
                if (!Object.Equals(DriveToThread,null))
                {
                    DriveToThread.Suspend();
                    DriveToThread = null;
                    Cancel = true;
                }

                MotorController.EmergencyBreak();
                MotorController.Break();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR WHILE DRIVER STOP:" + ex.Message);
            }
        }



        public static void DriveToTarget()
        {
            DriveTo(Target.Latitute + "," + Target.Longitute);
        }


        public static void Explore(int minDist = 20)
        {
            DriveToThread = new Thread(() =>
            {
                Console.WriteLine("Start exploring now");

                MotorController.MotorBlocked = false;
                MotorController.Steer((int)SteerDirection.Right);
                Thread.Sleep(1000);
                MotorController.Steer((int)SteerDirection.Left);
                Thread.Sleep(1000);
                MotorController.Steer((int)SteerDirection.Forwards);

                while (true)
                {
                    MotorController.Forward((int)MotorSpeeds.Slow);

                    while (CollisionProtection.GetDistances()[0] > minDist)
                    {
                        Thread.Sleep(50);
                    }

                    MotorController.Break();


                    MotorController.Reverse((int)MotorSpeeds.Slow);

                    while (CollisionProtection.GetDistances()[0] < 60 && CollisionProtection.GetDistances()[1] > minDist)
                    {
                        Thread.Sleep(50);
                    }

                    MotorController.Break();


                    MotorController.Steer((int)SteerDirection.Right);
                    MotorController.Forward((int)MotorSpeeds.Slow);

                    while (CollisionProtection.GetDistances()[0] > minDist && CollisionProtection.GetDistances()[0] > minDist)
                    {
                        Thread.Sleep(50);
                    }
                }

            });

            DriveToThread.Start();
        }




        /// <summary>
        /// Coords in DDD
        /// </summary>
        /// <param name="Coords"></param>
        public static void DriveTo(string Coords = "", int minDist = 50)
        {
            if (DriveToThread != null)
            {
                DriveToThread.Suspend();
                MotorController.EmergencyBreak();
            }

            Cancel = false;

            Thread.Sleep(2000);

            DriveToThread = new Thread(() =>
            {
                MotorController.MotorBlocked = false;

                Target = new GPSPoint(Convert.ToDouble(Coords.Split(';')[0], CultureInfo.InvariantCulture), Convert.ToDouble(Coords.Split(';')[1], CultureInfo.InvariantCulture));


                while (GPSController.CurrentPosition == null)
                {
                    Thread.Sleep(100);
                    Comunicator.SendToServer("Info;Waiting for current GPS Point");
                }


                while (!FunctionHelper.AboutRight(GPSController.CurrentPosition.Latitute, Target.Latitute, 0.00001) &&
                      !FunctionHelper.AboutRight(GPSController.CurrentPosition.Longitute, Target.Longitute, 0.00001))
                {

                    FreeVehicle(minDist);

                    AlignToTarget(GPSController.DegreeBearing(GPSController.CurrentPosition.Latitute, GPSController.CurrentPosition.Latitute, Target.Latitute, Target.Longitute));

                    MotorController.Forward((int)MotorSpeeds.Slow);

                    while (CollisionProtection.GetDistances()[0] > minDist)
                    {
                        Thread.Sleep(200);
                    }
                    MotorController.Break();


                    var headingbefore = Compass.Heading;

                    if (CollisionProtection.GetDistances().All(d => d < 1000))
                    {
                        //Links ist frei
                        if (CollisionProtection.GetDistances()[2] > minDist)
                        {
                            MotorController.Steer((int)SteerDirection.Left);

                            MotorController.Forward((int)MotorSpeeds.Slow);

                            while (Math.Atan2(Math.Sin(headingbefore - Compass.Heading), Math.Cos(headingbefore - Compass.Heading)) > 90)
                            {
                                //So lange Fahren bis der anfangswinkel 90° vom endwinkel ist
                                Thread.Sleep(200);
                            }

                            MotorController.Break();

                        }
                        //Rechts ist frei
                        else if (CollisionProtection.GetDistances()[3] > minDist)
                        {
                            MotorController.Steer((int)SteerDirection.Right);

                            MotorController.Forward((int)MotorSpeeds.Slow);

                            while (Math.Atan2(Math.Sin(headingbefore - Compass.Heading), Math.Cos(headingbefore - Compass.Heading)) > 90)
                            {
                                //So lange Fahren bis der anfangswinkel 90° vom endwinkel ist
                                Thread.Sleep(200);
                            }

                            MotorController.Break();

                        }
                        //NUR hinten frei
                        else if (CollisionProtection.GetDistances()[1] > minDist)
                        {
                            MotorController.Steer((int)SteerDirection.Forwards);
                            MotorController.Reverse((int)MotorSpeeds.Slow);
                        }
                    }
                }
            });

            DriveToThread.Start();
        }

        private static void FreeVehicle(int minDist=20)
        {
            var lastdistances = CollisionProtection.GetDistances();

            while (CollisionProtection.GetDistances().Any(d => d < minDist) && !Cancel)
            {
                if ((CollisionProtection.GetDistances()[2] > minDist))
                    MotorController.Steer((int)SteerDirection.Left);
                else if ((CollisionProtection.GetDistances()[3] > minDist))
                    MotorController.Steer((int)SteerDirection.Right);

                else if ((CollisionProtection.GetDistances()[1] > minDist))
                    MotorController.Steer((int)SteerDirection.Forwards);
                else if ((CollisionProtection.GetDistances()[0] > minDist))
                    MotorController.Steer((int)SteerDirection.Forwards);

                if (CollisionProtection.GetDistances()[0] > minDist && CollisionProtection.GetDistances()[1] > minDist)
                {
                    Random random = new Random(DateTime.Now.Second);

                    switch (random.Next(0, 1))
                    {
                        case 0:
                            MotorController.Forward((int)MotorSpeeds.Slow);
                            break;

                        case 1:
                            MotorController.Reverse((int)MotorSpeeds.Slow);
                            break;
                    }
                }
                else if (CollisionProtection.GetDistances()[0] > minDist)
                    MotorController.Forward((int)MotorSpeeds.Slow);
                else if (CollisionProtection.GetDistances()[1] > minDist)
                    MotorController.Reverse((int)MotorSpeeds.Slow);
                else
                    throw new Exception("I'M STUCK!!!");

                while (lastdistances.Count(d => d < minDist) != CollisionProtection.GetDistances().Count(d => d < minDist)&& !Cancel)
                {
                    Thread.Sleep(200);
                }

                MotorController.Break();

            }
        }


        private static void AlignToTarget(double TargetAngle)
        {
            if (Math.Sin(Compass.Heading - TargetAngle) > 0)
            {
                MotorController.Steer((int)SteerDirection.Right);
            }
            else
            {
                MotorController.Steer((int)SteerDirection.Left);
            }

            MotorController.Forward((int)MotorSpeeds.Slow);


            while (!FunctionHelper.AboutRight(Compass.Heading, TargetAngle, 10) && !Cancel)
            {
                if (CollisionProtection.GetDistances().Any(d => d < 100))
                {
                    MotorController.Break();
                    FreeVehicle(50);
                    MotorController.Forward((int)MotorSpeeds.Slow);
                }
            }

            MotorController.Break();
        }


        private static void AlignParallel(string d, double Distance)
        {
            if (d == "l")
            {
                MotorController.Steer((int)SteerDirection.Left);


            }
            else if (d == "r")
            {

                while (CollisionProtection.GetDistances()[0] + (CarInfo.Width / 2) < CarInfo.TurningCircleRadius)
                {
                    MotorController.Reverse((int)MotorSpeeds.Slow);
                }
                MotorController.Break();

                var frontdiff = CollisionProtection.GetDistances()[0];

                MotorController.Steer((int)SteerDirection.Right);

                while(CollisionProtection.GetDistances()[3]> Distance)
                {

                }
            }
        }
    }
}
