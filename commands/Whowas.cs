﻿using System;
using System.IO;
using System.Data.SQLite;
using iconnect;

namespace commands
{
    class Whowas
    {
        private static String DataPath { get; set; }

        public static void Setup()
        {
            DataPath = Path.Combine(Server.DataPath, "whowas.db");

            if (!File.Exists(DataPath))
                CreateDatabase();
            else
            {
                int time = (int)(Server.Time - 1209600);

                using (SQLiteConnection connection = new SQLiteConnection("Data Source=\"" + DataPath + "\""))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand("delete from whowas where jointime<@jointime", connection))
                    {
                        command.Parameters.Add(new SQLiteParameter("@jointime", time));
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void Add(IUser client)
        {
            int time = (int)(Server.Time - 1);

            using (SQLiteConnection connection = new SQLiteConnection("Data Source=\"" + DataPath + "\""))
            {
                connection.Open();
                bool exists = false;

                String query = @"select * from whowas where name=@name and ip=@ip";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.Add(new SQLiteParameter("@name", client.Name));
                    command.Parameters.Add(new SQLiteParameter("@ip", client.ExternalIP.ToString()));

                    using (SQLiteDataReader reader = command.ExecuteReader())
                        if (reader.Read())
                            exists = true;
                }

                if (!exists)
                {
                    query = @"insert into whowas (name, version, ip, jointime) 
                                 values (@name, @version, @ip, @jointime)";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.Add(new SQLiteParameter("@name", client.Name));
                        command.Parameters.Add(new SQLiteParameter("@version", client.Version));
                        command.Parameters.Add(new SQLiteParameter("@ip", client.ExternalIP.ToString()));
                        command.Parameters.Add(new SQLiteParameter("@jointime", time));
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    query = @"update whowas set jointime=@jointime where name=@name and ip=@ip";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.Add(new SQLiteParameter("@name", client.Name));
                        command.Parameters.Add(new SQLiteParameter("@ip", client.ExternalIP.ToString()));
                        command.Parameters.Add(new SQLiteParameter("@jointime", time));
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void Query(IUser admin, String q)
        {
            String query = @"select * from whowas
                             where name like @name
                             or ip like @ip
                             order by jointime desc
                             limit 50";

            using (SQLiteConnection connection = new SQLiteConnection("Data Source=\"" + DataPath + "\""))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.Add(new SQLiteParameter("@name", "%" + q + "%"));
                    command.Parameters.Add(new SQLiteParameter("@ip", q + "%"));
                    bool exists = false;

                    using (SQLiteDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            exists = true;
                            String name = (String)reader["name"];
                            String version = (String)reader["version"];
                            String ip = (String)reader["ip"];
                            int time = (int)reader["jointime"];

                            admin.Print(Template.Text(Category.WhoWas, 0).Replace("+n",
                                name).Replace("+v", version).Replace("+ip", ip).Replace("+t", Helpers.UnixTimeToDateString((uint)time)));
                        }

                    if (!exists)
                        admin.Print(Template.Text(Category.WhoWas, 1).Replace("+n", q));
                }
            }
        }

        private static void CreateDatabase()
        {
            SQLiteConnection.CreateFile(DataPath);

            using (SQLiteConnection connection = new SQLiteConnection("Data Source=\"" + DataPath + "\""))
            {
                connection.Open();

                String query = @"create table whowas
                                 (
                                     name text not null,
                                     version text not null,
                                     ip text not null,
                                     jointime int not null
                                 )";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    command.ExecuteNonQuery();
            }
        }
    }
}
