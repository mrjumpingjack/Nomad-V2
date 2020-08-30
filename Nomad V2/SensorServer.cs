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
        static List<ClientHandler> ClientHandlers = new List<ClientHandler>();

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

                     ClientHandler clientHandler = new ClientHandler();
                     clientHandler.startClient(clientSocket, Convert.ToString(counter));
                     clientHandler.DataRecived += Client_DataRecived;
                     clientHandler.lastCallCheckTimer.Elapsed += LastCallCheckTimer_Elapsed;
                     clientHandler.lastCallCheckTimer.Start();
                     ClientHandlers.Add(clientHandler);
                 }

                 clientSocket.Close();
                 serverSocket.Stop();
                 Console.WriteLine(" >> " + "exit");
                 Console.ReadLine();
             });

            listenThread.Start();
            listenThread.IsBackground = true;

        }

        private static void LastCallCheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                ((CustomTimer)sender).Handler.lastCallCheckTimer.Stop();

                var clientHandler = ClientHandlers.First(c => c.ClientRole == ((CustomTimer)sender).ClientRole);

                if (clientHandler.MaxDownTime == -1)
                    return;

                if (clientHandler.LastCall < DateTime.Now.AddSeconds(-clientHandler.MaxDownTime))
                {
                    Console.WriteLine(clientHandler.ClientRole + " SEEMS TO BE DOWN!!!");

                    //Console.WriteLine(DataWrangler.RunningClients.Count);

                    //foreach (var cl in DataWrangler.RunningClients)
                    //{
                    //    Console.WriteLine(cl.Key);
                    //    Console.WriteLine(cl.Value);
                    //}

                    var runningrunningclients = DataWrangler.RunningClients.Where(sc => !sc.Value.HasExited);
                    Console.WriteLine(runningrunningclients.Count());


                    Console.WriteLine(String.Join(",", (runningrunningclients.Select(rc => rc.Key + ":" + rc.Value))));
                    Console.WriteLine(runningrunningclients.First(sc => sc.Key == clientHandler.ClientRole).Value);


                    Console.WriteLine("KILLING: " + runningrunningclients.First(sc => sc.Key == clientHandler.ClientRole).Value);
                    runningrunningclients.First(sc => sc.Key == clientHandler.ClientRole).Value.Kill();


                    Console.WriteLine("REMOVING " + clientHandler.ClientRole + " FROM LIST OF CLIENTS");
                    DataWrangler.RunningClients.Remove(clientHandler.ClientRole);
                    clientHandler.Dispose();

                    Console.WriteLine("REMOVING " + clientHandler.ClientRole + " FROM LIST OF HANDLERS");
                    ClientHandlers.Remove(clientHandler);

                    Console.WriteLine("Restarting Client....");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            ((CustomTimer)sender).Handler.lastCallCheckTimer.Start();
        }
    

        public static bool SendToClient(string role, string data)
        {
            try
            {
                if(ClientHandlers.Count==0)
                {
                    Console.WriteLine("NO CLIENTS ONLINE!!!!");
                    return false;
                }


                if (!data.StartsWith("<SOT>"))
                    data = "<SOT>" + data;

                if (!data.EndsWith("<EOT>"))
                    data = data + "<EOT>";


                ClientHandlers.First(c => c.ClientRole == role).Send(data);
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

            if (((ClientHandler)sender).ClientRole.ToLower().Contains("wheelrotation"))
            {
                WheelDataRecived.Invoke(null, e);
            }
            else if (((ClientHandler)sender).ClientRole.ToLower().Contains("sonar"))
            {
                SonarDataRecived.Invoke(null, e);

            }
            else if (((ClientHandler)sender).ClientRole.ToLower().Contains("compass"))
            {
                CompassDataRecived.Invoke(null, e);
            }
        }
    }

    public class ClientHandler
    {
        public event EventHandler<string> DataRecived;
        public string ClientRole = "Unknown";
        public int MaxDownTime = 5;

        public CustomTimer lastCallCheckTimer = new CustomTimer(3000);

        public DateTime LastCall;

        TcpClient clientSocket;
        string clNo;
        NetworkStream networkStream;

        Thread ListenThread;

        public void startClient(TcpClient inClientSocket, string clientNo)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clientNo;

            networkStream = clientSocket.GetStream();
            Console.WriteLine("Client connected:" + clientNo);
            ListenThread = new Thread(Listen);
            ListenThread.Start();
            ListenThread.IsBackground = true;
        }


        public void Dispose()
        {
            lastCallCheckTimer.Stop();
            clientSocket.Close();
            networkStream.Close();
            ListenThread.Suspend();
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
                            lastCallCheckTimer.ClientRole = ClientRole;
                            lastCallCheckTimer.Handler = this;
                            Console.WriteLine("Client role is " + ClientRole);
                            SpecialSetup();
                        }
                        else
                        {
                            DataRecived.Invoke(this, dataFromClient);
                        }

                        if (DataWrangler.Verbose)
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


        private void SpecialSetup()
        {
            if(ClientRole=="Steering")
            {
                //Prevent steering.py restart
                MaxDownTime = -1;
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

    public class CustomTimer : System.Timers.Timer
    {
        public string ClientRole;
        public ClientHandler Handler;


        public CustomTimer(int interval)
        {
            this.Interval = interval;
        }
    }
}
