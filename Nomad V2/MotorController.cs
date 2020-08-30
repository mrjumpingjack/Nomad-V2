using System;
using System.Threading;

namespace Nomad_V2
{

    public static class MotorController
    {
        public static Direction Direction = Direction.Hold;


        private static double CurrentSpeed = 0;

        private static int CurrentAngle = 6;
        private static int TargetAngle = 6;

        public static int curSpeed
        {
            get
            {
                return (int)CurrentSpeed;
            }
        }


        public static double TargetSpeed = 0;
        public static double MaxSpeed = -1;

        private static double DynamicSpeed = 0;


        public static bool MotorBlocked { get; set; }

        public static Thread MotorControllThread;

        public static void Init()
        {
            MotorControllThread = new Thread(() =>
            {
                while (true)
                {
                    if (MaxSpeed != -1)
                        if (TargetSpeed > MaxSpeed)
                            TargetSpeed = MaxSpeed;
                
                    if ((CurrentSpeed == TargetSpeed && (CurrentSpeed != 0 ? WheelRotation.Rotations != 0 : true)) && CurrentAngle == TargetAngle)
                    {
                        Thread.Sleep(100);
                        continue;
                    }


                    if (MotorBlocked)
                    {
                        CurrentSpeed = 0;
                        TargetSpeed = 0;
                        TargetAngle = 6;

                        SensorServer.SendToClient("Steering", Direction.ToString() + "," + (Direction == Direction.Forwards ? (int)CurrentSpeed : ((int)CurrentSpeed * -1)) + "," + CurrentAngle);
                        continue;
                    }


                    double TargetSpeedDifference = ((double)TargetSpeed - (double)CurrentSpeed);

                    if (DataWrangler.Verbose)
                    {
                        Console.WriteLine("Roation:" + WheelRotation.Rotations);
                        Console.WriteLine("TargetSpeed:" + TargetSpeed);
                    }

                    //if the vehicle is not moving accelarate slowly to protect the gears
                    if (WheelRotation.Rotations == 0)
                    {
                        if (Math.Abs(CurrentSpeed) >= Math.Abs(TargetSpeed) && TargetSpeed != 0)
                        {
                            //if the wheels dont turn, we need more speed
                            Console.WriteLine("Ts reached; no rotation; accelerating");
                            CurrentSpeed += Math.Round(CurrentSpeed / 10, 2);
                        }
                        else
                        {
                            CurrentSpeed += (TargetSpeedDifference / (double)10);
                            CurrentSpeed = Math.Round(CurrentSpeed, 2);
                        }
                    }
                    else
                    {
                        //if we are already moving just set the speed
                        CurrentSpeed = TargetSpeed;
                    }

                    //Set moving direction
                    if (CurrentSpeed < 0)
                        Direction = Direction.Reverse;
                    else if (CurrentSpeed > 0)
                        Direction = Direction.Forwards;
                    else if (CurrentSpeed == 0)
                        Direction = Direction.Hold;

                    if (Math.Abs(CurrentSpeed) < 0.9)
                        CurrentSpeed = 0;


                    int SignalSpeed = (int)CurrentSpeed;

                    if (DataWrangler.Verbose)
                        Console.WriteLine("CurrentSpeed:" + CurrentSpeed);

                    switch (Direction)
                    {
                        case Direction.Hold:
                            SignalSpeed = 0;
                            break;
                        case Direction.Forwards:
                            SignalSpeed = (int)CurrentSpeed;
                            break;
                        case Direction.Reverse:
                            SignalSpeed = (int)CurrentSpeed * -1;
                            break;
                    };

                    if(DataWrangler.Verbose)
                        Console.WriteLine("Steering " + Direction.ToString() + "," + SignalSpeed + "," + CurrentAngle);

                    SensorServer.SendToClient("Steering", Direction.ToString() + "," + SignalSpeed + "," + CurrentAngle);

                    TargetAngle = CurrentAngle;

                    Thread.Sleep(50);
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
            CurrentAngle = angle;
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

        public static void EmergencyBreak(string Reason)
        {

            if (Direction == Direction.Forwards)
            {
                for (int i = 0; i < 50; i++)
                {
                    MotorController.Reverse(Convert.ToInt32(-CurrentSpeed * 2));
                    Thread.Sleep(50);
                }
            }
            else if (Direction == Direction.Reverse)
            {
                MotorController.Forward(Convert.ToInt32(CurrentSpeed * 2));
                Thread.Sleep(200);

                MotorController.Forward(Convert.ToInt32(CurrentSpeed));
                Thread.Sleep(200);
            }

            SensorServer.SendToClient("Steering", "Breaking,0");
            MotorBlocked = true;

            Console.WriteLine("!!!EMERGENCY BREAK!!! " + Reason);

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
