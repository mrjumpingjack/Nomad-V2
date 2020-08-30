using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControllPanel
{
    public class Comunicator
    {
        public event EventHandler<string> DataRecived;


        NetworkStream stream;
        public void Connect(String serverAdd)
        {
            TcpListener server = null;
            try
            {
                Thread comThread = new Thread(() =>
                {
                    // Set the TcpListener on port 13013.
                    Int32 port = 13013;
                    IPAddress localAddr = IPAddress.Parse(serverAdd);

                    // TcpListener server = new TcpListener(port);
                    server = new TcpListener(localAddr, port);

                    // Start listening for client requests.
                    server.Start();

                    // Buffer for reading data
                    Byte[] bytes = new Byte[256];
                    String data = null;


                    // Enter the listening loop.
                    while (true)
                    {
                        Console.Write("Waiting for a connection... ");
                        TcpClient client = server.AcceptTcpClient();
                  
                        Console.WriteLine("Connected!");

                        data = null;

                        // Get a stream object for reading and writing
                        stream = client.GetStream();

                        Send("Mode;0");

                        int i;

                        // Loop to receive all the data sent by the client.
                        try
                        {
                            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                // Translate data bytes to a ASCII string.
                                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                Console.WriteLine("Received: {0}", data);

                                DataRecived?.Invoke(this, data);

                                //// Process the data sent by the client.
                                //data = data.ToUpper();

                                //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                                //// Send back a response.
                                //stream.Write(msg, 0, msg.Length);
                                //Console.WriteLine("Sent: {0}", data);

                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                });

                comThread.IsBackground = true;
                comThread.Start();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        public void Send(string data)
        {
            if (stream == null)
                return;

            try
            {
                if (!data.StartsWith("<SOT>"))
                    data = "<SOT>" + data;

                if (!data.EndsWith("<EOT>"))
                    data += "<EOT>";

                Console.WriteLine(data);

                byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR WHILE SENDEING TO SERVER: " + ex.Message) ;
            }
        }
    }
}
