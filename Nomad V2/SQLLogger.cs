using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
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
                while (true)
                {
                    Thread.Sleep(1000);
                    Log(WheelRotation.SpeedinKMh, Compass.Heading, GPSController.CurrentPosition.ToString(),  CollisionProtection.GetDistancesAsString(), Driver.Target.ToString(), DataWrangler.OPMode.ToString());
                }
            });

            loggingThread.Start();
            loggingThread.IsBackground = true;
        }

        public static void CreateDB()
        {
            if (File.Exists("Logging.db"))
            {
                Console.WriteLine("DB exists");
                return;
            }

            SqliteConnection.CreateFile("Logging.db");

            SqliteConnection m_dbConnection = new SqliteConnection("Data Source=Logging.db;Version=3;");
            m_dbConnection.Open();


            string sql = "create table Logging (ID int(20), SessionID int(20), LogTime DATETIME DEFAULT CURRENT_TIMESTAMP, Speed Double(4), Heading Double(4),Position string(20),Distances string(30), Target string(20), Mode string(25))";

            SqliteCommand command = new SqliteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            m_dbConnection.Close();
        }

        public static void Log(double Speed, double heading, String Position, string distances, String target, string mode)
        {
            if (!File.Exists("Logging.db"))
            {
                Console.WriteLine("DB does not exists");
                CreateDB();
            }

            SqliteConnection m_dbConnection = new SqliteConnection("Data Source=Logging.db;Version=3;");
            m_dbConnection.Open();

            string sql = "INSERT INTO Logging (SessionID, Speed, Heading, Position,Distances,Target,Mode)" +
                         "VALUES(" + SessionID + "," + Speed + "," + heading + ",'" + Position.Replace(',',';') + "','" + distances.Replace(',', ';') + "','" + target.Replace(',', ';') + "','" + mode + "')";

            SqliteCommand command = new SqliteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            m_dbConnection.Close();
        }

        public static int GetSessionID()
        {
            if (!File.Exists("Logging.db"))
            {
                Console.WriteLine("DB does not exists");
                return 0;
            }

            SqliteConnection m_dbConnection = new SqliteConnection("Data Source=Logging.db;Version=3;");
            m_dbConnection.Open();


            string sql = "SELECT SessionID FROM Logging DESC LIMIT 1";

            SqliteCommand command = new SqliteCommand(sql, m_dbConnection);


            if (!command.ExecuteReader().HasRows)
            {
                m_dbConnection.Close();
                return 0;
            }

            SqliteCommand Selectcommand = new SqliteCommand(sql, m_dbConnection);

            var result = Selectcommand.ExecuteReader();

            var r = Convert.ToInt32(result.GetValue(0));

            m_dbConnection.Close();

            return r;
        }

    }
}
