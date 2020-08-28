using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Nomad_V2
{
    public static class SQLLogger
    {
        private static int SessionID = -1;

        public static void Init()
        {
            Console.WriteLine("LOOGING STARTED");
            CreateDB();

            SessionID = GetSessionID() + 1;

            Thread loggingThread = new Thread(() => 
            {
                Thread.Sleep(1000);

                Log(WheelRotation.SpeedinKMh, Compass.Heading, GPSController.CurrentPosition.ToString(), string.Join(",", CollisionProtection.GetDistances()), Driver.Target.ToString(), Convert.ToInt32(DataWrangler.OPMode.ToString()));

            });

            loggingThread.Start();
            loggingThread.IsBackground = true;
        }

        public static void CreateDB()
        {
            if (File.Exists("Logging.sqlite"))
            {
                Console.WriteLine("DB exists");
                return;
            }

            SQLiteConnection.CreateFile("Logging.sqlite");

            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=Logging.sqlite;Version=3;");
            m_dbConnection.Open();



            string sql = "create table Logging (ID int(20), SessionID int(20), LogTime DateTime, Speed Double(4), Heading Double(4),Position string(20),Distances string(30), Target string(20), Mode int(2)";

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            m_dbConnection.Close();
        }

        public static void Log(double Speed, double heading, String Position, string distances, String target, int mode)
        {
            if (!File.Exists("Logging.sqlite"))
            {
                Console.WriteLine("DB does not exists");
                CreateDB();
            }

            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=Logging.sqlite;Version=3;");
            m_dbConnection.Open();

            string sql = "INSERT INTO Logging (SessionID, Speed, Heading, Position,Distances,Target,Mode)" +
                         "VALUES(" + SessionID + "," + heading + "," + Position + "," + distances + "," + target + "," + mode + ")";


            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            m_dbConnection.Close();
        }

        public static int GetSessionID()
        {
            if (!File.Exists("Logging.sqlite"))
            {
                Console.WriteLine("DB does not exists");
                return 0;
            }

            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=Logging.sqlite;Version=3;");
            m_dbConnection.Open();

            string sql = "SELECT SessionID FROM Logging DESC LIMIT 1";

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);

            var result = command.ExecuteReader().GetInt32(0);
            m_dbConnection.Close();

            return result;
        }

    }
}
