using System;
using System.Threading;

namespace Nomad_V2
{

    public static class MotorController
    {
        public static Direction Direction = new Direction();

        private static double CurrentSpeed = 0;
        public static int curSpeed
        {
            get
            {
                return (int)CurrentSpeed;
            }
        }


        public static double TargetSpeed = 0;
        public static double MaxSpeed = 0;

        private static double DynamicSpeed = 0;


        public static bool MotorBlocked { get; set; }

        public static Thread MotorControllThread;

        public static void Init()
        {
            MotorControllThread = new Thread(() =>
            {
                while (true)
                {

                    //Console.WriteLine(Direction.ToString() + ":Soll:" + TargetSpeed + "Ist:" + CurrentSpeed);


                    if (CurrentSpeed == TargetSpeed || MotorBlocked)
                        continue;

                    double TargetSpeedDifference = ((double)TargetSpeed - (double)CurrentSpeed);


                    CurrentSpeed += (TargetSpeedDifference / (double)10);


                    if (CurrentSpeed < 0)
                        Direction = Direction.Reverse;
                    else if (CurrentSpeed > 0)
                        Direction = Direction.Forwards;
                    else if (CurrentSpeed == 0)
                        Direction = Direction.Hold;

                   
                    SensorServer.SendToClient("Steering", Direction.ToString() + "," + (int)CurrentSpeed);
                }
            });

            MotorControllThread.Start();
            MotorControllThread.IsBackground = true;
        }

        public static void Kill()
        {
            MotorControllThread.Suspend();
        }

        public static void Steer(int angle)
        {
            SensorServer.SendToClient("Steering", "Steer," + angle);
        }

        public static void Forward(int speed)
        {
            TargetSpeed = speed;
        }

        public static void Reverse(int speed)
        {
            TargetSpeed = speed;
        }

        public static void Break()
        {
            TargetSpeed = 0;
        }

        public static void EmergencyBreak()
        {
            MotorBlocked = true;

            if (Direction == Direction.Forwards)
            {
                MotorController.Reverse(100);
                Thread.Sleep(200);
            }
            else if (Direction == Direction.Reverse)
            {
                MotorController.Forward(100);
                Thread.Sleep(200);
            }

            SensorServer.SendToClient("Steering", "Breaking,0");

            Console.WriteLine("!!!EMERGENCY BREAK!!!");
        }


        public static void Exelarate(int s)
        {
            TargetSpeed = s;

            //if (s > 0)
            //    Forward(s);
            //else if (s < 0)
            //    Reverse(s * -1);
            //else
            //    Break();
        }
    }

    public enum Direction
    {
        Hold,
        Forwards = 1,
        Reverse = 2
    }

    public enum SteerDirection
    {
        Forwards = 6,
        Left = 1,
        Right = 10
    }

    public enum MotorSpeeds
    {
        Slow = 25,
        Modarate = 40,
        Fast = 80,
        SuperFast = 1000
    }
}
