using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class Comunicator
    {
        static NetworkStream stream;
        static TcpClient client;
        static Thread beakonThread;
        static Thread comThread;

        static string Server;

        public static event EventHandler<string> ServerDataRecived;

        public static void Connect(String server)
        {
            try
            {
                Server = server;

                if (beakonThread!=null && beakonThread.ThreadState == ThreadState.Running)
                    beakonThread.Suspend();

                beakonThread = new Thread(() => { SendBeakonDirect(); });

                comThread = new Thread(() =>
                {
                    try
                    {
                        Int32 port = 13013;

                        string message = "<SOT> " + "Connected to: " + Server + " at port " + port + "<EOT>";
                        Console.WriteLine("Connecting to: " + Server + " at port " + port);

                        client = new TcpClient(server, port);
                        stream = client.GetStream();

                        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                        stream.Write(data, 0, data.Length);

                        Console.WriteLine("Sent: {0}", message);
                        data = new Byte[256];

                        beakonThread.Start();
                        beakonThread.IsBackground = true;
                        Console.WriteLine("Beakon started");

                        while (client.Connected)
                        {
                            //Console.WriteLine("Reading stream...");
                            String responseData = String.Empty;
                            Int32 bytes = stream.Read(data, 0, data.Length);
                            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);


                            foreach (var resData in responseData.Split(new string[] { "<SOT>", "<EOT>" },StringSplitOptions.RemoveEmptyEntries))
                            {
                                //Console.WriteLine(resData);
                                ServerDataRecived?.Invoke(null, resData);
                            }
                        }

                        Console.WriteLine("SERVER CONNECTION LOST, RECONNECTING");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR IN COM THREAD:" + ex);
                        Connect(Server);
                    }
                });

                if (comThread != null && comThread.ThreadState == ThreadState.Running)
                    comThread.Suspend();

                comThread.Start();
                comThread.IsBackground = true;

            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);

                if (!client.Client.Connected)
                {
                    comThread.Suspend();
                    beakonThread.Suspend();
                    Connect(Server);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR WHILE COMUNICATING: {0}", ex);
            }
        }

        public static void SendBeakonDirect()
        {
            while (client.Connected)
            {
                try
                {

                    SendToServer("Status;" + GPSController.CurrentPosition.ToString() + ";" + Compass.Heading + ";" + CollisionProtection.GetDistancesAsString() + ";" + WheelRotation.Rotations + ";" + DateTime.Now);


                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR WHILE SENDING BEAKON:" + ex.Message);
                }

                Thread.Sleep(1000);
            }
        }


        public static void SendToServer(string message)
        {
            try
            {
                if (client.Client.Connected)
                {
                    if (!message.StartsWith("<SOT>"))
                        message = "<SOT>" + message;

                    if (!message.EndsWith("<EOT>"))
                        message += "<EOT>";

                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);

                    String responseData = String.Empty;
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                }
            }
            catch (Exception)
            {
                if (!client.Client.Connected)
                {
                    comThread.Suspend();
                    beakonThread.Suspend();
                    Connect(Server);
                }
            }

        }
    }
}
