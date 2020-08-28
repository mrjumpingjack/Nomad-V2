using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class SensorServer
    {
        static List<handleClinet> Clients = new List<handleClinet>();

        public static event EventHandler<string> SonarDataRecived;
        public static event EventHandler<string> WheelDataRecived;
        public static event EventHandler<string> CompassDataRecived;

        public static bool Cancel = false;
        
        public static void Init()
        {
            TcpListener serverSocket = new TcpListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 13014));
            TcpClient clientSocket = default(TcpClient);
            int counter = 0;

            serverSocket.Start();
            Console.WriteLine(" >> " + "Server Started");

            Thread listenThread = new Thread(() =>
             {
                 while (!Cancel)
                 {
                     counter += 1;
                     clientSocket = serverSocket.AcceptTcpClient();
                     Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
                     handleClinet client = new handleClinet();
                     client.startClient(clientSocket, Convert.ToString(counter));
                     client.DataRecived += Client_DataRecived;
                     Clients.Add(client);
                 }

                 clientSocket.Close();
                 serverSocket.Stop();
                 Console.WriteLine(" >> " + "exit");
                 Console.ReadLine();
             });

            listenThread.Start();
            listenThread.IsBackground = true;

        }


        public static bool SendToClient(string role, string data)
        {
            try
            {
                if(Clients.Count==0)
                {
                    Console.WriteLine("NO CLIENTS ONLINE!!!!");
                    return false;
                }


                if (!data.StartsWith("<SOT>"))
                    data = "<SOT>" + data;

                if (!data.EndsWith("<EOT>"))
                    data = data + "<EOT>";


                Clients.First(c => c.ClientRole == role).Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR WHILE SENDING TO CLIENT:"+ex.Message);

                return false;
            }

            return true;
        }



        private static void Client_DataRecived(object sender, string e)
        {

            if (((handleClinet)sender).ClientRole.ToLower().Contains("wheelrotation"))
            {
                WheelDataRecived.Invoke(null, e);
            }
            else if (((handleClinet)sender).ClientRole.ToLower().Contains("sonar"))
            {
                SonarDataRecived.Invoke(null, e);

            }
            else if (((handleClinet)sender).ClientRole.ToLower().Contains("compass"))
            {
                CompassDataRecived.Invoke(null, e);
            }
        }
    }

    public class handleClinet
    {
        public bool Verbose = false;
        public event EventHandler<string> DataRecived;
        public string ClientRole = "Unknown";

        System.Timers.Timer lastCallCheckTimer = new System.Timers.Timer(3000);

        DateTime LastCall;

        TcpClient clientSocket;
        string clNo;
        NetworkStream networkStream;

        public void startClient(TcpClient inClientSocket, string clientNo)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clientNo;

            networkStream = clientSocket.GetStream();
            Console.WriteLine("Client connected:" + clientNo);
            Thread ctThread = new Thread(Listen);
            ctThread.Start();
            ctThread.IsBackground = true;

            lastCallCheckTimer.Elapsed += LastCallCheckTimer_Elapsed;
            lastCallCheckTimer.Start();
        }

        private void LastCallCheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (LastCall < DateTime.Now.AddSeconds(5))
            {
                Console.WriteLine(ClientRole + " SEEMS TO BE DOWN!!!");
                DataWrangler.StartedClients.First(sc => sc.Key == ClientRole).Value.Kill();
                FunctionHelper.StartClientProcesses(ClientRole);
            }
        }

        private void Listen()
        {
            byte[] bytesFrom = new byte[1024];
            string dataFromClient;


            while ((true))
            {
                try
                {
                    if (networkStream == null)
                        continue;

                    int read = networkStream.Read(bytesFrom, 0, bytesFrom.Length);
                    if (read > 0)
                    {
                        dataFromClient = Encoding.ASCII.GetString(bytesFrom);

                        dataFromClient = dataFromClient.Split(new string[] { "<SOT>", "<EOT>" }, StringSplitOptions.RemoveEmptyEntries)[0];

                        if (dataFromClient.StartsWith("ClientRole="))
                        {
                            ClientRole = dataFromClient.Substring("ClientRole=".Length);
                            Console.WriteLine("Client role is " + ClientRole);
                        }
                        else
                        {
                            DataRecived.Invoke(this, dataFromClient);
                        }

                        if (Verbose)
                            Console.WriteLine("RAW:" + ClientRole + ":" + dataFromClient);

                        LastCall = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR:" + ex.ToString());
                }
            }
        }


        public void Send(string data)
        {
            Byte[] sendBytes = null;

            sendBytes = Encoding.ASCII.GetBytes(data);
            networkStream.Write(sendBytes, 0, sendBytes.Length);
            networkStream.Flush();
        }
    }

}
