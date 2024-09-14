using LogAnalysis.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading;

namespace LogAnalysis.Utils
{
    internal class SQLiteHelper
    {

        readonly WorkTimer timer;
        static readonly Lazy<SQLiteHelper> instance = new Lazy<SQLiteHelper>(() => new SQLiteHelper(), LazyThreadSafetyMode.PublicationOnly);

        public static SQLiteHelper Instance => instance.Value;

        ConcurrentQueue<LogModel> logs = new ConcurrentQueue<LogModel>();

        private SQLiteHelper()
        {
            timer = new WorkTimer(Consts.SQL_TIMER_INTERVAL);
            timer.Excute = SaveData;
            timer.StartTimer();
        }

        private void SaveData()
        {
            if (logs.Count > 0)
            {
                var currDay = DateTime.Today.ToString("yyyy_MM_dd");
                var filterLogDir = FilePathUtils.GetFilterDir();
                var db_file = Path.Combine(filterLogDir, Consts.DB_FILTER);
                var db_con = OpenSQLite(db_file);
                while (logs.Count > 0)
                {
                    db_con = SwitchConnection(db_con, ref currDay);
                    if (logs.TryDequeue(out var log))
                    {
                        InsertData(db_con, Consts.DB_TABLE, log);
                    }
                }
            }
        }

        private SQLiteConnection SwitchConnection(SQLiteConnection oldCon, ref string oldDayStr)
        {
            var oldDay = DateTime.Parse(oldDayStr.Replace("_", "-"));
            if (DateTime.Today > oldDay)
            {
                var filterLogDir = FilePathUtils.GetFilterDir();
                var db_file = Path.Combine(filterLogDir, Consts.DB_FILTER);
                oldCon.Close();
                oldCon = OpenSQLite(db_file);
                oldDayStr = DateTime.Today.ToString("yyyy_MM_dd");
            }
            return oldCon;
        }

        private bool CreateTable(SQLiteConnection con, string tableName)
        {
            bool flag = false;
            if (CheckIfTableExists(con, tableName))
            {
                flag = true;
            }
            else
            {
                try
                {
                    var createStr = $"CREATE TABLE {tableName}(id INTEGER PRIMARY KEY, boardid TEXT, system TEXT, module TEXT,log TEXT,date TEXT)";
                    using var cmd = new SQLiteCommand(createStr, con);
                    cmd.ExecuteNonQuery();
                    flag = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return flag;
        }

        private bool CheckIfTableExists(SQLiteConnection con, string tableName)
        {
            bool flag = false;
            try
            {
                string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";

                using (SQLiteCommand command = new SQLiteCommand(query, con))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        flag = reader.HasRows;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return flag;
        }

        private bool InsertData(SQLiteConnection con, string tableName, LogModel data)
        {
            bool flag = false;
            try
            {
                if (!CheckIfTableExists(con, tableName))
                {
                    CreateTable(con, tableName);
                }
                var instertStr = $"INSERT INTO {tableName}(boardid, system, module, log, date) VALUES('{data.BoardID}','{data.System}', '{data.Module}', '{data.Log}', '{data.Date}')";
                using var cmd = new SQLiteCommand(instertStr, con);
                cmd.ExecuteNonQuery();
                flag = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return flag;
        }

        public static SQLiteConnection OpenSQLite(string path)
        {
            SQLiteConnection con;
            try
            {
                var cs = "Data Source=" + path;
                con = new SQLiteConnection(cs);
                con.Open();
            }
            catch (Exception ex)
            {
                con = null;
                Console.WriteLine(ex.Message);
            }
            return con;
        }

        public void InsertData(LogModel data)
        {
            this.logs.Enqueue(data);
        }

        public static List<string> QueryData(SQLiteConnection con, string tableName, string boardId, string systemName, string moduleName)
        {
            List<string> reult = new List<string>();
            try
            {
                var queryStr = $"SELECT * FROM {tableName} where boardid=@boardid AND system=@system AND module=@module";
                using var cmd = new SQLiteCommand(queryStr, con);
                cmd.Parameters.AddWithValue("@boardid", boardId);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@module", moduleName);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 读取并处理查询结果
                        var board = reader["boardId"].ToString();
                        var system = reader["system"].ToString();
                        var module = reader["module"].ToString();
                        var date = reader["date"].ToString();
                        var log = reader["log"].ToString();
                        reult.Add($"[{date}] [{module}] {log}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return reult;
        }
    }
}
