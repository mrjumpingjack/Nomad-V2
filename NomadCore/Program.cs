using System;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;

namespace NomadCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Pi.Init<BootstrapWiringPi>();


            var motorpin1 = Pi.Gpio[17];
            var motorpin2 = Pi.Gpio[27];

            var motorpin3 = Pi.Gpio[23];
            var motorpin4 = Pi.Gpio[24];

            motorpin1.PinMode = GpioPinDriveMode.Output;
            motorpin2.PinMode = GpioPinDriveMode.Output;

            motorpin3.PinMode = GpioPinDriveMode.Output;
            motorpin4.PinMode = GpioPinDriveMode.Output;

            var isOn = false;
            while (true)
            {
                isOn = !isOn;
                motorpin1.Write(isOn);
                motorpin2.Write(!isOn);

                motorpin3.Write(isOn);
                motorpin4.Write(!isOn);


                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
