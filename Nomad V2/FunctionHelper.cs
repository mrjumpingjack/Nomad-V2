using GMap.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad_V2
{
    public static class FunctionHelper
    {
        /// <summary>
        /// if it doesn't need to be perfect
        /// </summary>
        public static bool AboutRight(double a, double b, double plusMinus)
        {
            if (a >= b- plusMinus && a <= b + plusMinus)
                return true;
            else 
                return false;
        }


        public static void StartClientProcesses(string clientname)
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (File.ReadAllText("/proc/" + p.Id.ToString() + "/cmdline").Contains(clientname))
                        p.Kill();
                }
                catch (Exception ex)
                {

                }
            }

            Process ClientProcess = new Process();
            ClientProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ClientProcess.StartInfo.UseShellExecute = false;
            ClientProcess.StartInfo.FileName = "sudo";
            ClientProcess.StartInfo.Arguments = "python " + clientname + " 13014";
            ClientProcess.StartInfo.RedirectStandardError = true;
            ClientProcess.StartInfo.RedirectStandardOutput = true;
            ClientProcess.Start();

            DataWrangler.StartedClients.Add(clientname, ClientProcess);

            Console.WriteLine("Starting:" + clientname);
        }

    }
}
