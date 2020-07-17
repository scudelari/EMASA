using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Emasa_Geometry_Optimizer.GHVars;
using Emasa_Geometry_Optimizer.Properties;

namespace Emasa_Geometry_Optimizer
{
    public static class DBHelpers
    {
        public static string DatabaseLocation = null;

        public static SQLiteConnectionStringBuilder ConnString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DatabaseLocation)) throw new Exception("Cannot get the SQLite's connection string if the DatabaseLocation is null or empty.");
                if (!File.Exists(DatabaseLocation)) throw new Exception($"The SQLite database file {DatabaseLocation} does not exist.");

                return new SQLiteConnectionStringBuilder { DataSource = DatabaseLocation };;
            }
        }

        // Convert an object to a byte array
        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null) return null;

            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        // Convert a byte array to an Object
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            if (arrBytes == null) return null;

            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                object obj = binForm.Deserialize(memStream);
                return obj;
            }
        }

        public static void CreateNewDBDefinition(bool inOverwrite = false)
        {
            if (inOverwrite) 
                if (File.Exists(DatabaseLocation)) File.Delete(DatabaseLocation);

            if (File.Exists(DatabaseLocation)) return;

            SQLiteConnection.CreateFile(DatabaseLocation);

            using (SQLiteConnection sqliteConn = new SQLiteConnection(ConnString.ConnectionString))
            {
                sqliteConn.Open();

                using (SQLiteTransaction transaction = sqliteConn.BeginTransaction())
                {
                    using (SQLiteCommand cmd = sqliteConn.CreateCommand())
                    {
                        cmd.CommandText = Resources.CreateDatabaseSQL;
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }

                sqliteConn.Close();
            }
        }

        public static List<GHInputParameterDefinition> Get_GHInputParameterDefinition()
        {
            DataTable table = Get_Table("GHInputParameterDefinition");

            List<GHInputParameterDefinition> toRet = new List<GHInputParameterDefinition>();
            toRet.AddRange(from a in table.AsEnumerable()
                           select new GHInputParameterDefinition(a.Field<string>("Type"), a.Field<string>("Name"), a.Field<byte[]>("Min"), a.Field<byte[]>("Max")));
            
            return toRet;
        }

        public static List<GHOutputParameterDefinition> Get_GHOutputParameterDefinition()
        {
            DataTable table = Get_Table("GHOutputParameterDefinition");

            List<GHOutputParameterDefinition> toRet = new List<GHOutputParameterDefinition>();
            toRet.AddRange(from a in table.AsEnumerable()
                select new GHOutputParameterDefinition(a.Field<string>("Type"), a.Field<string>("Name")));
            return toRet;
        }

        public static DataTable Get_Table(string inTableName, SQLiteConnection inConnection = null)
        {
            DataTable dt = new DataTable();

            // If the connection is not given, creates a new one to do the action
            if (inConnection == null)
            {
                using (SQLiteConnection sqliteConn = new SQLiteConnection(ConnString.ConnectionString))
                {
                    sqliteConn.Open();

                    using (SQLiteCommand cmd = sqliteConn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT * FROM {inTableName};";
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        dt.Load(reader);
                    }

                    sqliteConn.Close();
                }
            }
            else // Otherwise, uses the given connection
            {
                if (inConnection.State != ConnectionState.Open) throw new Exception("The database connection must be open.");

                using (SQLiteCommand cmd = inConnection.CreateCommand())
                {
                    cmd.CommandText = $"SELECT * FROM {inTableName};";
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    dt.Load(reader);
                }
            }

            return dt;
        }
    }
}
