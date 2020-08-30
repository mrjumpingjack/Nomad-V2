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

        public static GPSPoint Target { get; set; } = new GPSPoint(-1,-1);
        public static bool Cancel { get;  set; }

        static Random dirrnd = new Random(DateTime.Now.Second);



        public static void Init()
        {
            GPSController.GPSNotAvailable += GPSController_GPSNotAvailable;
        }

        private static void GPSController_GPSNotAvailable(object sender, EventArgs e)
        {
            Stop();
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

                MotorController.EmergencyBreak("Driver stopped");
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

        /// <summary>
        /// Just explore the environment,dodge obsticles
        /// </summary>
        /// <param name="minDist"></param>
        public static void Explore(int minDist = 20)
        {
            DriveToThread = new Thread(() =>
            {
                Console.WriteLine("Start exploring now");

                //FREE THE WHALES, I...I mean motor
                MotorController.MotorBlocked = false;

                //Show that indeed we are in explroring mode
                MotorController.Steer((int)SteerDirection.Right);
                Thread.Sleep(500);
                MotorController.Steer((int)SteerDirection.Left);
                Thread.Sleep(500);
                MotorController.Steer((int)SteerDirection.Forwards);


                while (true)
                {
                    //1. Drive forwards as long we can
                    MotorController.Forward((int)MotorSpeeds.Slow);

                    while (CollisionProtection.Sensors[0].Distance > minDist)
                    {
                        Thread.Sleep(50);
                    }
                    MotorController.Break();

                    //2. We hit something; Stop; Reverse,so we can make turn
                    MotorController.Reverse((int)MotorSpeeds.Slow);

                    while (CollisionProtection.Sensors[0].Distance < 60 && CollisionProtection.Sensors[1].Distance > minDist)
                    {
                        Thread.Sleep(50);
                    }
                    MotorController.Break();


                    //3. Randomly choose a direction(which is free) to turn to
                    var rnddir = dirrnd.Next(2, 3);
                    var heading = Compass.Heading;

                    while (CollisionProtection.Sensors[rnddir].Distance < 60)
                    {
                        rnddir = dirrnd.Next(2, 3);
                        Thread.Sleep(100);
                    }

                    if (rnddir == 2)
                        MotorController.Steer((int)SteerDirection.Left);
                    else if (rnddir == 3)
                        MotorController.Steer((int)SteerDirection.Right);

                    //4. Make a 90° turn
                    MotorController.Forward((int)MotorSpeeds.Slow);
                    while (CollisionProtection.Sensors[1].Distance < minDist && CollisionProtection.Sensors[0].Distance > minDist && (Math.Atan2(Math.Sin(heading - Compass.Heading), Math.Cos(heading - Compass.Heading)) < 90))
                    {
                        Thread.Sleep(50);
                    }

                    MotorController.Steer((int)SteerDirection.Forwards);
                }
            });

            DriveToThread.Start();
            DriveToThread.IsBackground = true;
        }




        /// <summary>
        /// Drive to target; Coords in DDD
        /// </summary>
        /// <param name="Coords"></param>
        public static void DriveTo(string Coords = "", int minDist = 50)
        {
            if (DriveToThread != null)
            {
                DriveToThread.Suspend();
                MotorController.EmergencyBreak("Drive to changed");
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

                    while (CollisionProtection.Sensors[0].Distance > minDist)
                    {
                        Thread.Sleep(200);
                    }
                    MotorController.Break();


                    var headingbefore = Compass.Heading;

                    if (CollisionProtection.Sensors.All(d => d.Distance < 1000))
                    {
                        //Links ist frei
                        if (CollisionProtection.Sensors[1].Distance > minDist)
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
                        else if (CollisionProtection.Sensors[3].Distance > minDist)
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
                        else if (CollisionProtection.Sensors[1].Distance > minDist)
                        {
                            MotorController.Steer((int)SteerDirection.Forwards);
                            MotorController.Reverse((int)MotorSpeeds.Slow);
                        }
                    }
                }
            });

            DriveToThread.Start();
            DriveToThread.IsBackground = true;
        }

        private static void FreeVehicle(int minDist=20)
        {
            var lastdistances = CollisionProtection.Sensors.Select(s=>s.Distance);

            while (CollisionProtection.Sensors.Any(d => d.Distance < minDist) && !Cancel)
            {
                if ((CollisionProtection.Sensors[2].Distance > minDist))
                    MotorController.Steer((int)SteerDirection.Left);
                else if ((CollisionProtection.Sensors[3].Distance > minDist))
                    MotorController.Steer((int)SteerDirection.Right);

                else if ((CollisionProtection.Sensors[1].Distance > minDist))
                    MotorController.Steer((int)SteerDirection.Forwards);
                else if ((CollisionProtection.Sensors[0].Distance > minDist))
                    MotorController.Steer((int)SteerDirection.Forwards);

                if (CollisionProtection.Sensors[0].Distance > minDist && CollisionProtection.Sensors[1].Distance > minDist)
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
                else if (CollisionProtection.Sensors[0].Distance > minDist)
                    MotorController.Forward((int)MotorSpeeds.Slow);
                else if (CollisionProtection.Sensors[1].Distance > minDist)
                    MotorController.Reverse((int)MotorSpeeds.Slow);
                else
                    throw new Exception("I'M STUCK!!!");

                while (lastdistances.Count(d => d < minDist) != CollisionProtection.Sensors.Count(d => d.Distance < minDist)&& !Cancel)
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
                if (CollisionProtection.Sensors.Any(d => d.Distance < 100))
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

                while (CollisionProtection.Sensors[0].Distance + (CarInfo.Width / 2) < CarInfo.TurningCircleRadius)
                {
                    MotorController.Reverse((int)MotorSpeeds.Slow);
                }
                MotorController.Break();

                var frontdiff = CollisionProtection.Sensors[0].Distance;

                MotorController.Steer((int)SteerDirection.Right);

                while(CollisionProtection.Sensors[3].Distance > Distance)
                {

                }
            }
        }
    }
}
