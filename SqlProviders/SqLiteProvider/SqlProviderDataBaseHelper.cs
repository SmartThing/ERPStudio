﻿using System;
using System.Data;
using System.Data.SQLite;
using SqlProxyProvider;

namespace SqlProvider
{
    class SqlProviderDataBaseHelper : SqlProxyProvider.ISqlProxyDataBaseHelper
    {
        public string DataSource { get; set; } = "";
        public string UserID { get; set; } = "";
        public string InitialCatalog { get; set; } = "";
        public string Password { get; set; } = "";
        public bool IntegratedSecurity { get; set; } = false;

        public void CreateDatabase()
        {
            CreateDatabase(DataSource);
        }

        public void CreateDatabase(string connectionString)
        {
            SQLiteConnection.CreateFile(connectionString);
        }

        public string QuerySearchTable(string tableName) => $"select tbl_name from sqlite_master where type = 'table' and tbl_name = '{tableName}";

        public bool SearchColumn(string srcTable, string srcColumn, System.Data.IDbConnection connection)
        {
            var found = false;
            try
            {
                var command = $"PRAGMA table_info('{srcTable}');";

                using (var cmd = new SQLiteCommand(command, connection as SQLiteConnection))
                {

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader.GetString(reader.GetOrdinal("name"));
                            if (name == srcColumn)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return true;
            }
            return found;
        }
    }
}
