using GMap.NET;
using GMap.NET.MapProviders;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GMap.NET.WindowsPresentation;
using System.IO;
using Vlc.DotNet;
using Vlc.DotNet.Wpf;

namespace ControllPanel
{

    public enum OPMode
    {
        Direct = 0,
        Automatic = 1
    };




    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public OPMode OPMode = OPMode.Direct;

        Comunicator comunicator = new Comunicator();
        DataHelper DataHelper = new DataHelper();
        GMapMarker VehicleMarker;

        static XInputController Controller = new XInputController();

        public bool SendControllerInput { get; set; } = true;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = DataHelper;

            var vlcLibDirectory = new DirectoryInfo(System.IO.Path.Combine(@"C:\Program Files (x86)\VideoLAN\VLC"));

            var options = new string[]
            {
                 "--video-filter=transform",
                 "--transform-type=vflip",
            };

            MyControl.BeginInit();
            MyControl.EndInit();

            this.MyControl.SourceProvider.CreatePlayer(vlcLibDirectory, options);

            // Load libvlc libraries and initializes stuff. It is important that the options (if you want to pass any) and lib directory are given before calling this method.
            this.MyControl.SourceProvider.MediaPlayer.Play("rtsp://192.168.0.148:8554/");

            InitializeComponent();

            comunicator.Connect(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip=>ip.ToString().StartsWith("192.168.0")).ToString());
            comunicator.DataRecived += Comunicator_DataRecived;
            DataHelper.CurrentVehiclePosChanged += DataHelper_CurrentCarPosChnaged;
            DataHelper.CurrentVehicleHeadingChnaged += DataHelper_CurrentVehicleHeadingChnaged;

            MainMap.MapProvider = GMapProviders.GoogleMap;
            MainMap.Position = new PointLatLng(52.331730, 9.214540);
            MainMap.Zoom = 10;
            MainMap.CenterCrossPen = new Pen(Brushes.Transparent, 0);
            MainMap.ShowCenter = false;
            MainMap.ShowTileGridLines = false;


            Thread ControllerThread = new Thread(() =>
            {
                while (true)
                {
                    GetControllerInput();
                }

            });
            ControllerThread.Start();
            ControllerThread.IsBackground = true;
        }


        public void GetControllerInput()
        {
            //if controller state changed
            Controller.Update();

            {
                if (SendControllerInput)
                {
                    comunicator.Send("Direct;" + Convert.ToInt32(Math.Round(Convert.ToDouble(mapControllerToSteering(Controller.leftThumb.X), CultureInfo.InvariantCulture))) + ";" +
                        Convert.ToInt32(Math.Round(Convert.ToDouble(mapControllerToSpeed(Controller.rightTrigger - Controller.leftTrigger), CultureInfo.InvariantCulture))));

                    //Console.WriteLine("Direct;" + Convert.ToInt32(Math.Round(Convert.ToDouble(mapControllerToSteering(Controller.leftThumb.X), CultureInfo.InvariantCulture))) + ";" +
                    //    Convert.ToInt32(Math.Round(Convert.ToDouble(mapControllerToSpeed(Controller.rightTrigger - Controller.leftTrigger), CultureInfo.InvariantCulture))));
                    Thread.Sleep(200);
                }
            }
        }

        private double mapControllerToSteering(double x)
        {
            return x.Map(-100,100, 1, 10);
        }

        private double mapControllerToSpeed(double x)
        {
            return x.Map(-255, 255, -100, 100);
        }

        private void DataHelper_CurrentVehicleHeadingChnaged(object sender, EventArgs e)
        {
            ((Arrow)VehicleMarker.Shape).RenderTransform = new RotateTransform(DataHelper.CurrentCarHeading, 100, 100);
        }

        private void DataHelper_CurrentCarPosChnaged(object sender, EventArgs e)
        {
            if (DataHelper.GoToCarPosOnUpdate == true)
                MainMap.Position = DataHelper.CurrentCarPos;

            if (VehicleMarker == null)
            {
                VehicleMarker = new GMapMarker(DataHelper.CurrentCarPos);
                VehicleMarker.Shape = new Arrow();
                ((Arrow)VehicleMarker.Shape).RenderTransform = new RotateTransform(DataHelper.CurrentCarHeading, 100, 100);
                MainMap.Markers.Add(VehicleMarker);
            }

            VehicleMarker.Position = DataHelper.CurrentCarPos;
        }

        private void Comunicator_DataRecived(object sender, string Data)
        {
            Dispatcher.Invoke(() =>
            {
                var Messages = Data.Split(new string[] { "<SOT>" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string msg in Messages)
                {
                    if (!msg.EndsWith("<EOT>"))
                        continue;



                    var msgc = msg.Substring(0, msg.Length - "<SOT>".Length);

                    var MSGParts = msgc.Split(';');

                    switch (MSGParts[0])
                    {
                        case "Status":
                            if (!String.IsNullOrEmpty(MSGParts[1]))
                                DataHelper.CurrentCarPos = new PointLatLng(
                                    Convert.ToDouble(MSGParts[1].Split(',')[0], CultureInfo.InvariantCulture),
                                    Convert.ToDouble(MSGParts[1].Split(',')[1], CultureInfo.InvariantCulture));

                            DataHelper.CurrentCarHeading = Convert.ToDouble(MSGParts[2], CultureInfo.InvariantCulture);

                            DataHelper.Distances = MSGParts[3].Split(',').Select(int.Parse).ToArray();

                            DataHelper.Rotations = Convert.ToInt32(MSGParts[4]);

                            break;


                        case "Setup":
                            switch (MSGParts[1])
                            {
                                case "Sensor":

                                    switch (MSGParts[2])
                                    {
                                        case "Sonar":

                                            switch (MSGParts[3])
                                            {
                                                case "Enabled":
                                                    SensorDock.Children.OfType<StackPanel>().First(s => s.Tag.ToString() == MSGParts[4]).Children[1].Visibility = (MSGParts[5] == "1" ? Visibility.Collapsed : Visibility.Visible);
                                                    break;
                                            }
                                            break;
                                    }
                                    break;
                            }
                            break;

                        default:

                            break;
                    }
                }
            });
        }

        private static string DMMToDMS(string CP)
        {
            var parts = CP.Split(';');

            var latdeg = parts[0].Split(new string[] { "° " }, StringSplitOptions.RemoveEmptyEntries)[0];
            var latm = parts[0].Split(new string[] { "° " }, StringSplitOptions.RemoveEmptyEntries)[1].Split('.')[0];
            var latmm = "0." + parts[0].Split('.')[1].TrimEnd('\'');


            var longdeg = parts[1].Split(new string[] { "° " }, StringSplitOptions.RemoveEmptyEntries)[0];
            var longm = parts[1].Split(new string[] { "° " }, StringSplitOptions.RemoveEmptyEntries)[1].Split('.')[0];
            var longmm = "0." + parts[1].Split('.')[1].TrimEnd('\'');

            string lat = latdeg + "° " + latm + "'" + Convert.ToString((Convert.ToDouble(latmm, CultureInfo.InvariantCulture) * 60)).Replace(',', '.');
            string lo = longdeg + "° " + longm + "'" + Convert.ToString((Convert.ToDouble(longmm, CultureInfo.InvariantCulture) * 60)).Replace(',', '.');

            string CurrentPosition = lat + ";" + lo.TrimStart(' ').TrimStart('0');
            return CurrentPosition;
        }

        private static string DMSToDMM(string CP)
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

        private static string DDDToDMS(string CP)
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



        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private string FixConutation(String indata)
        {
            var parts = indata.Split(',');

            var latdeg = parts[1].Substring(0, 2);
            var latmm = parts[1].Substring(2, parts[1].Length - 2);

            var longdeg = parts[3].Substring(0, 3);
            var longmm = parts[3].Substring(3, parts[1].Length - 2);


            string lat = latdeg + "° " + latmm + "'";
            string lon = longdeg + "° " + longmm + "'";

            //Console.WriteLine(lat);
            //Console.WriteLine(lon);


            return lat + ";" + lon;
        }

        private void On_BtnGoHere(object sender, RoutedEventArgs e)
        {
            GeoCoderStatusCode gcsc = new GeoCoderStatusCode();
            PointLatLng t = GMapProviders.GoogleMap.GetPoint(TB_Target.Text,out gcsc).GetValueOrDefault();
        }

        private void GoHere_Click(object sender, RoutedEventArgs e)
        {
            if (OPMode == OPMode.Automatic)
            {
                SendControllerInput = false;

                DataHelper.Target = MainMap.FromLocalToLatLng(Convert.ToInt32(Mouse.GetPosition(MainMap).X), Convert.ToInt32(Mouse.GetPosition(MainMap).Y));

                TB_Target.Text = DataHelper.Target.ToString();

                var t = DataHelper.Target.ToString().Split(',')[0].Substring("{Lat=".Length) + "." + DataHelper.Target.ToString().Split(',')[1]
                     + "," +
                     DataHelper.Target.ToString().Split(',')[2].Substring("Lng=".Length + 1) + "." + DataHelper.Target.ToString().Split(',')[3].Trim('}');

                comunicator.Send("Target;" + t);
            }
        }

        private void On_ModeAuto(object sender, RoutedEventArgs e)
        {
            OPMode = OPMode.Automatic;
        }

        private void On_ModeDirect(object sender, RoutedEventArgs e)
        {
            OPMode = OPMode.Direct;
        }

        private void On_BtnExplore(object sender, RoutedEventArgs e)
        {
            if (BtnExplore.Content.ToString() == "Start exploring")
            {
                if (OPMode == OPMode.Automatic)
                {
                    SendControllerInput = false;

                    comunicator.Send("Mode;1");

                    comunicator.Send("Explore;1");
                    BtnExplore.Content = "Stop exploring";
                }
            }
            else
            {
                comunicator.Send("Explore;0");
                comunicator.Send("Mode;0");

                BtnExplore.Content = "Start exploring";
                SendControllerInput = true;

            }


        
        }

        private void On_ToogleIgnoreSensor(object sender, MouseButtonEventArgs e)
        {
            int SID = Convert.ToInt32(((StackPanel)sender).Tag.ToString());

            comunicator.Send("Setup;Sensor;Sonar;Enabled;" + SID);
        }
    }


    public static class ExtensionMethods
    {
        public static double Map(this double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            double res= (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
            return res;
        }
    }

}
