using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class GPSController
    {
        public static bool isRunning = false;
        public static bool outputToConsole = false;

        public static event EventHandler GPSNotAvailable;
        static int NotAvailableCounter = 0;

        static SerialPort mySerialPort;


        public static GPSPoint CurrentPosition { get; set; } =new GPSPoint(-1,-1);

        public static void Init(string serialPortName = "/dev/ttyS0")
        {
            mySerialPort = new SerialPort(serialPortName);

            mySerialPort.BaudRate = 9600;
            mySerialPort.Parity = Parity.None;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;

            mySerialPort.Open();

            Console.WriteLine("GPS Serial initialized: " + mySerialPort.BaudRate);


            Thread GPSThread = new Thread(() =>
            {
                String indata = "";

                int nullround = 0;

                while (true)
                {
                    try
                    {
                        try
                        {
                            indata = mySerialPort.ReadLine();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ERROR WHILE READING GPSDATA:"+ ex.Message);
                        }
                        
                        Thread.Sleep(200);

                        if (indata == "")
                        {
                            if (nullround < 5)
                                nullround++;
                            else
                            {
                                try
                                {
                                    nullround = 0;
                                    mySerialPort.Close();
                                    Thread.Sleep(2000);
                                    mySerialPort.Open();

                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                        else
                        {
                            nullround = 0;

                            if (indata.Contains("GPGLL"))
                            {

                                try
                                {
                                    if (indata.Contains(",,,"))
                                    {
                                        throw new Exception("GPS ERROR GPLL IS EMPTY");
                                    }

                                    var parts = indata.Split(',');

                                    if (outputToConsole)
                                    {
                                        Console.WriteLine(indata);
                                        Console.WriteLine(parts[1]);
                                        Console.WriteLine(parts[3]);
                                    }

                                    var GPS = FixConutation(indata);

                                    var pos = DMMToDDD(GPS).Split(';');
                                    Console.WriteLine(String.Join(";", pos));

                                    CurrentPosition = new GPSPoint(Convert.ToDouble(pos[0], CultureInfo.InvariantCulture), Convert.ToDouble(pos[1], CultureInfo.InvariantCulture));

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("ERROR WHILE PARSING GPSDATA:" + ex.Message);
                                    NotAvailableCounter++;

                                    if (NotAvailableCounter > 10)
                                        GPSNotAvailable?.Invoke(null, null);
                                }

                                if (outputToConsole)
                                    Console.WriteLine("CP: " + CurrentPosition);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR WHILE COMPUTING GPSDATA:" + ex.Message);
                    }
                }
            });

            GPSThread.Start();
            GPSThread.IsBackground = true;
        }


        private static string FixConutation(String indata)
        {
            var parts = indata.Split(',');

            var latdeg = parts[1].Substring(0, 2);
            var latmm = parts[1].Substring(2, parts[1].Length - 2);

            var longdeg = parts[3].Substring(0, 3);
            var longmm = parts[3].Substring(3, parts[1].Length - 2);


            string lat = latdeg + "° " + latmm + "'";
            string lon = longdeg + "° " + longmm + "'";

            return lat + ";" + lon;
        }


        public static string DMMToDMS(string CP)
        {
            var parts = CP.Split(';');

            var latdeg = parts[0].Substring(0, 2);
            var latm = parts[0].Split('.')[0].Substring(latdeg.Length);
            var latmm = "0." + parts[0].Split('.')[1];


            var longdeg = parts[1].Substring(0, 4);
            var longm = parts[1].Split('.')[0].Substring(longdeg.Length);
            var longmm = "0." + parts[1].Split('.')[1];






            string lat = latdeg + "° " + latm + "'" + Convert.ToString((Convert.ToDouble(latmm, CultureInfo.InvariantCulture) * 60)).Replace(',', '.');
            string lo = longdeg + "° " + longm + "'" + Convert.ToString((Convert.ToDouble(longmm, CultureInfo.InvariantCulture) * 60)).Replace(',', '.');

            string CurrentPosition = lat + ";" + lo.TrimStart(' ').TrimStart('0');
            return CurrentPosition;
        }

        public static string DMSToDMM(string CP)
        {
            var parts = CP.Split(';');

            var latdeg = parts[0].Split('°')[0];
            var latm = parts[0].Split(new String[] { "'" }, StringSplitOptions.RemoveEmptyEntries)[0].Substring(latdeg.Length).Substring(("° ").Length);
            var latmm = parts[0].Split(new String[] { "'" }, StringSplitOptions.RemoveEmptyEntries)[1];


            var longdeg = parts[1].Split('°')[0];
            var longm = parts[1].Split(new String[] { "'" }, StringSplitOptions.RemoveEmptyEntries)[0].Substring(longdeg.Length).Substring(("° ").Length);
            var longmm = parts[1].Split(new String[] { "'" }, StringSplitOptions.RemoveEmptyEntries)[1];



            string lat = latdeg + "° " + Convert.ToString(Convert.ToDouble(latm, CultureInfo.InvariantCulture) + (Convert.ToDouble(latmm, CultureInfo.InvariantCulture) / 60)).Replace(",", ".");
            string lo = longdeg + "° " + Convert.ToString(Convert.ToDouble(longm, CultureInfo.InvariantCulture) + (Convert.ToDouble(longmm, CultureInfo.InvariantCulture) / 60)).Replace(",", ".");

            string CurrentPosition = lat + ";" + lo.TrimStart(' ').TrimStart('0');
            return CurrentPosition;
        }

        private static string DMMToDDD(string CP)
        {
            var parts = CP.Split(';');

            var latdeg = parts[0].Split('°')[0];
            var latm = parts[0].Split('°')[1].TrimStart(' ').TrimEnd('\'');


            var longdeg = parts[1].Split('°')[0];
            var longm = parts[1].Split('°')[1].TrimStart(' ').TrimEnd('\'');



            string lat = (Convert.ToDouble(latdeg, CultureInfo.InvariantCulture) + (Convert.ToDouble(latm, CultureInfo.InvariantCulture) / 60)).ToString().Replace(",", ".");
            string lo = (Convert.ToDouble(longdeg, CultureInfo.InvariantCulture) + (Convert.ToDouble(longm, CultureInfo.InvariantCulture) / 60)).ToString().Replace(",", ".");

            string CurrentPosition = lat + ";" + lo.TrimStart(' ');
            return CurrentPosition;
        }

        public static string DDDToDMS(string CP)
        {
            var parts = CP.Split(';');

            var latdeg = parts[0].Split('.')[0];
            var latm = "0." + parts[0].Split('.')[1].TrimStart(' ');


            var longdeg = parts[1].Split('.')[0];
            var longm = "0." + parts[1].Split('.')[1].TrimStart(' ');



            string lat = latdeg + "° " + (Convert.ToDouble(latm, CultureInfo.InvariantCulture) * 60).ToString().Replace(",", ".");
            string lo = longdeg + "° " + (Convert.ToDouble(longm, CultureInfo.InvariantCulture) * 60).ToString().Replace(",", ".");

            string CurrentPosition = lat + ";" + lo.TrimStart(' ');
            return CurrentPosition;
        }

        public static double DegreeBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRad(lon2 - lon1);
            var dPhi = Math.Log(
                Math.Tan(ToRad(lat2) / 2 + Math.PI / 4) / Math.Tan(ToRad(lat1) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        public static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (ToDegrees(radians) + 360) % 360;
        }

        public static double DegreesToRadians(double angle)
        {
            return angle * Math.PI / 180.0d;
        }
    }

    public class GPSPoint
    {
        public GPSPoint(double lati, double longi)
        {
            Latitute = lati;
            Longitute = longi;
        }

        public override string ToString()
        {
            return Latitute + "," + Longitute;
        }


        public double Latitute { get; set; }
        public double Longitute { get; set; }
    }
}