using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            List<handleClinet> Clients = new List<handleClinet>();

            TcpListener serverSocket = new TcpListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 13013));
            TcpClient clientSocket = default(TcpClient);
            int counter = 0;

            serverSocket.Start();
            Console.WriteLine(" >> " + "Server Started");


            while (true)
            {
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
                handleClinet client = new handleClinet();
                client.startClient(clientSocket, Convert.ToString(counter));
            }

            clientSocket.Close();
            serverSocket.Stop();
            Console.WriteLine(" >> " + "exit");
            Console.ReadLine();
        }
    }

    public class handleClinet
    {
        public event EventHandler<string> DataRecived;


        TcpClient clientSocket;
        string clNo;
        NetworkStream networkStream;

        public void startClient(TcpClient inClientSocket, string clinedNo)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clinedNo;
            networkStream = clientSocket.GetStream();
            Console.WriteLine("Client connected:"+ clinedNo);
            Thread ctThread = new Thread(Listen);
            ctThread.Start();
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
                        dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                        Console.WriteLine(dataFromClient);
                    }

                    
                    //DataRecived.Invoke(this, dataFromClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR:" + ex.ToString());
                }
            }
        }


        public  void Send(string data)
        {
            Byte[] sendBytes = null;
            string serverResponse = null;

            sendBytes = Encoding.ASCII.GetBytes(data);
            networkStream.Write(sendBytes, 0, sendBytes.Length);
            networkStream.Flush();
            Console.WriteLine(" >> " + serverResponse);
        }
    }

}
