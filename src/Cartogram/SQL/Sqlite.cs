﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cartogram.SQL
{
    public static class Sqlite
    {
        private static readonly string _dbPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Cartogram\MapsDB.s3db";
        private static SQLiteConnection Connection { get; set; }

        static Sqlite()
        {
            //var constring = $@"Data Source={_dbPath};Version=3;";
            var constring = new SQLiteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Version = 3,
            };
            Connection = new SQLiteConnection(constring.ToString());
            Connection.ParseViaFramework = true;
            if (SetupDb())
            {

            }
        }

        private static bool SetupDb()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Cartogram"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Cartogram");
                if (!File.Exists(_dbPath)) SQLiteConnection.CreateFile(_dbPath);
            }
            try
            {
                using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
                {
                    using (var cmd = new SQLiteCommand(connection))
                    {
                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `maps` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `mysql_id` INTEGER DEFAULT 0, `rarity` TEXT, `level` INTEGER, `name` TEXT, 
                                            `quality` INTEGER, `quantity` INTEGER, `started_at` DATETIME, `finished_at` DATETIME, `notes` TEXT, `league` TEXT, `character` TEXT, `unidentified` INTEGER NOT NULL,
                                            `ownmap` INTEGER NOT NULL, `item_rarity` INTEGER NOT NULL, `pack_size` INTEGER NOT NULL, `zana_mod` TEXT NOT NULL);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `affixes` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `map_id` INTEGER, `affix` TEXT);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `map_drops` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `map_id` INTEGER, `rarity` TEXT, 
                                        `level` INTEGER, `name` TEXT, `zana` TINYINT, `carto` TINYINT)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `currency_drops` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `map_id` INTEGER, `name` TEXT, `count` INTEGER)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `unique_drops` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `map_id` INTEGER, `name` TEXT)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `divination_drops` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `map_id` INTEGER, `name` TEXT, `count` INTEGER)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS divination_idx ON divination_drops(map_id, name)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE UNIQUE INDEX  IF NOT EXISTS currency_idx ON currency_drops(map_id, name);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `experience` (`level` INTEGER, `total_exp` INTEGER, `exp_goal` INTEGER);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `map_experience` (`map_id` INTEGER, `exp_before` INTEGER, `level_before` INTEGER, `percent_before` INTEGER, 
                                           `exp_after` INTEGER, `level_after` INTEGER, `percent_after` INTEGER);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `character_details` (`character_id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `map_information` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `unique` TINYINT, 
                                            `zone` TEXT, `boss` TEXT, `boss_information` TEXT, `description` TEXT);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS `error_log` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `message` TEXT, `inner_exception` TEXT, `stack_trace`, 
                                            `error_time` DATETIME, `environment` TEXT, `version` TEXT, `method` TEXT);";
                        cmd.ExecuteNonQuery();

                        return true;
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(@"Error: " + ex.Message);
                return false;
            }
        }


        public static int AddMap(Map newMap)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string addQuery = @"INSERT INTO `maps` (`rarity`, `level`, `name`, `quality`, `quantity`, `started_at`, `league`, `character`, `unidentified`, `ownmap`, `item_rarity`, `pack_size`, `zana_mod`) VALUES 
                                                             (@rarity, @level, @name, @quality, @quantity, @startedat, @league, @character, @unidentified, @ownmap, @itemrarity, @packsize, @zanamod)";
                using (var cmd = new SQLiteCommand(addQuery, connection))
                {
                    //cmd.Parameters.AddWithValue("mysqlid", newMap.SqlId);
                    cmd.Parameters.AddWithValue("rarity", newMap.Rarity);
                    cmd.Parameters.AddWithValue("level", newMap.Level);
                    cmd.Parameters.AddWithValue("name", newMap.Name);
                    cmd.Parameters.AddWithValue("quality", newMap.Quality);
                    cmd.Parameters.AddWithValue("quantity", newMap.Quantity);
                    cmd.Parameters.AddWithValue("startedat", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("league", newMap.League);
                    cmd.Parameters.AddWithValue("character", newMap.Character);
                    cmd.Parameters.AddWithValue("unidentified", newMap.Unidentified);
                    cmd.Parameters.AddWithValue("ownmap", newMap.OwnMap);
                    cmd.Parameters.AddWithValue("itemrarity", newMap.ItemRarity);
                    cmd.Parameters.AddWithValue("packsize", newMap.PackSize);
                    cmd.Parameters.AddWithValue("zanamod", newMap.ZanaMod);

                    cmd.ExecuteNonQuery();
                }

                var mapId = (int)connection.LastInsertRowId;
                const string addAffixes = @"INSERT INTO `affixes` (`map_id`, `affix`) VALUES (@id, @affix)";
                using (var cmd = new SQLiteCommand(addAffixes, connection))
                {
                    cmd.Parameters.AddWithValue("id", connection.LastInsertRowId);
                    foreach (var affix in newMap.Affixes)
                    {
                        cmd.Parameters.AddWithValue("affix", affix);
                        cmd.ExecuteNonQuery();
                    }
                }
                //const string addExperience = @"INSERT INTO `map_experience` (`map_id`, `exp_before`, `level_before`, `percent_before`) VALUES (@id, @expb, @levelb, @percentb);";
                //using (var cmd = new SQLiteCommand(addExperience, connection))
                //{
                //    cmd.Parameters.AddWithValue("@id", mapId);
                //    cmd.Parameters.AddWithValue("@expb", newMap.ExpBefore.CurrentExperience);
                //    cmd.Parameters.AddWithValue("@levelb", newMap.ExpBefore.Level);
                //    cmd.Parameters.AddWithValue("@percentb", newMap.ExpBefore.Percentage);
                //    cmd.ExecuteNonQuery();
                //}

                return mapId;
            }
        }

        public static Map GetMap(int mapId)
        {
            var affixes = MapAffixes(mapId);
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                //const string queryMap = @"SELECT * from `maps` m1 JOIN `map_experience` e1 ON m1.id=e1.map_id WHERE m1.id=@id";
                const string queryMap = @"SELECT * from `maps` WHERE id=@id";
                using (var cmd = new SQLiteCommand(queryMap, connection))
                {
                    cmd.Parameters.AddWithValue("@id", mapId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id, level, quality, quantity, itemRarity, packsize, levelBefore, percentBefore, levelAfter, percentAfter;
                            //Int64 experienceBefore, experienceAfter;
                            //var expBefore = new Experience
                            //{
                            //    CurrentExperience = Int64.TryParse(reader["exp_before"].ToString(), out experienceBefore) ? experienceBefore : 0,
                            //    Level = int.TryParse(reader["level_before"].ToString(), out levelBefore) ? levelBefore : 0,
                            //    Percentage = int.TryParse(reader["percent_before"].ToString(), out percentBefore) ? percentBefore : 0,
                            //};
                            //var expAfter = new Experience
                            //{
                            //    CurrentExperience = Int64.TryParse(reader["exp_after"].ToString(), out experienceAfter) ? experienceAfter : 0,
                            //    Level = int.TryParse(reader["level_after"].ToString(), out levelAfter) ? levelAfter : 0,
                            //    Percentage = int.TryParse(reader["percent_after"].ToString(), out percentAfter) ? percentAfter : 0,
                            //};

                            DateTime startAt, finishAt;
                            return new Map
                            {
                                Id = int.TryParse(reader["id"].ToString(), out id) ? id : -1,
                                Rarity = reader.GetString(2),
                                Level = int.TryParse(reader["level"].ToString(), out level) ? level : -1,
                                Name = reader["name"].ToString(),
                                Quality = int.TryParse(reader["quality"].ToString(), out quality) ? quality : -1,
                                Quantity = int.TryParse(reader["quantity"].ToString(), out quantity) ? quantity : -1,
                                StartAt = DateTime.TryParse(reader["started_at"].ToString(), out startAt) ? startAt : new DateTime(0001, 01, 01),
                                FinishAt = DateTime.TryParse(reader["finished_at"].ToString(), out finishAt) ? finishAt : new DateTime(0001, 01, 01),
                                Notes = reader["notes"].ToString(),
                                //ExpAfter = expAfter,
                                //ExpBefore = expBefore,
                                Affixes = affixes,
                                League = reader["league"].ToString(),
                                Character = reader["character"].ToString(),
                                Unidentified = reader["unidentified"].ToString() == "1",
                                OwnMap = reader["ownmap"].ToString() == "1",
                                ItemRarity = int.TryParse(reader["item_rarity"].ToString(), out itemRarity) ? itemRarity : -1,
                                PackSize = int.TryParse(reader["pack_size"].ToString(), out packsize) ? packsize : -1,
                                ZanaMod = reader["zana_mod"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public static DataTable MapDataTable()
        {
            var dtMaps = new DataTable("maps");
            dtMaps.Columns.Add("id", typeof(int));
            dtMaps.Columns.Add("mysql_id", typeof(int));
            dtMaps.Columns.Add("level");
            dtMaps.Columns.Add("name");
            dtMaps.Columns.Add("gained", typeof(float));
            dtMaps.Columns.Add("zana_mod");
            dtMaps.Columns.Add("rarity");
            dtMaps.Columns.Add("quality");
            dtMaps.Columns.Add("quantity");
            dtMaps.Columns.Add("itemrarity");
            dtMaps.Columns.Add("packsize");
            dtMaps.Columns.Add("-");
            dtMaps.Columns.Add("even");
            dtMaps.Columns.Add("+1");
            dtMaps.Columns.Add("+2");

            try
            {
                using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
                {
                    const string selectQuery = @"SELECT * FROM `maps`";
                    using (var cmd = new SQLiteCommand(selectQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var mapId = int.Parse(reader["id"].ToString());
                                var sqlId = int.Parse(reader["mysql_id"].ToString());
                                var zanaMod = reader["zana_mod"].ToString();
                                dtMaps.Rows.Add(mapId, sqlId, int.Parse(reader["level"].ToString()),
                                    reader["name"].ToString(), $"{ExpGained(mapId):N2}", zanaMod.Length > 0 ? zanaMod.Remove(3, zanaMod.Length-3) : string.Empty,
                                    reader.GetString(2), int.Parse(reader["quality"].ToString()),
                                    int.Parse(reader["quantity"].ToString()), int.Parse(reader["item_rarity"].ToString()),
                                    int.Parse(reader["pack_size"].ToString()), MapDrops(mapId, "<",""), MapDrops(mapId, "=",""), 
                                    MapDrops(mapId, "=", "+1"), MapDrops(mapId, "=", "+2"));
                            }
                        }
                    }
                    return dtMaps;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return null;
            }
        }

        public static DataTable DropDataTable(int mapId)
        {
            var dtDrops = new DataTable("drops");
            dtDrops.Columns.Add("title");
            dtDrops.Columns.Add("drops");
            dtDrops.Columns.Add("maps");
            var drops = "";

            try
            {
                using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
                {
                    const string mapQuery = @"SELECT `level` FROM `map_drops` WHERE `map_id`=@mapid";
                    const string uniqueQuery = @"SELECT `name` FROM `unique_drops` WHERE `map_id`=@mapid";
                    const string currencyQuery = @"SELECT name, count FROM `currency_drops` WHERE `map_id`=@mapid";
                    const string divinationQuery = @"SELECT name, count FROM `divination_drops` WHERE `map_id`=@mapid;";

                    using (var cmd = new SQLiteCommand(connection))
                    {
                        cmd.CommandText = mapQuery;
                        cmd.Parameters.AddWithValue("mapid", mapId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                drops += reader["level"] + ", ";
                            }
                            if (drops.Length > 2) drops = drops.Remove(drops.Length - 2, 2);
                            var mapList = MapList(mapId);
                            var tooltipText = mapList.Aggregate("", (current, item) => current + (item.Key + " - " + item.Value + Environment.NewLine));
                            if (tooltipText.EndsWith(Environment.NewLine)) tooltipText = tooltipText.Remove(tooltipText.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                            dtDrops.Rows.Add("Maps", drops, tooltipText);
                        }

                        cmd.CommandText = uniqueQuery;
                        drops = "";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                drops += reader["name"] + ", ";
                            }
                            if (drops.Length > 2) drops = drops.Remove(drops.Length - 2, 2);
                            dtDrops.Rows.Add("Uniques", drops);
                        }

                        cmd.CommandText = currencyQuery;
                        drops = "";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                drops += reader["name"] + " x" + reader["count"] + ", ";
                            }
                            if (drops.Length > 2) drops = drops.Remove(drops.Length - 2, 2);
                            dtDrops.Rows.Add("Currency", drops);
                        }

                        cmd.CommandText = divinationQuery;
                        drops = "";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                drops += reader["name"] + " x" + reader["count"] + ", ";
                            }
                            if (drops.Length > 2) drops = drops.Remove(drops.Length - 2, 2);
                            dtDrops.Rows.Add("Divinations", drops);
                        }
                    }
                    return dtDrops;
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static List<DataTable> DropTables()
        {
            var listTables = new List<DataTable>();

            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string mapsRunQuery = @"SELECT * FROM `maps`";
                const string mapQuery = @"SELECT map_id, rarity, level, name, zana, carto FROM `map_drops`;";
                const string uniqueQuery = @"SELECT map_id, name FROM `unique_drops`;";
                const string currencyQuery = @"SELECT map_id, name, count FROM `currency_drops`;";
                const string divinationQuery = @"SELECT map_id, name, count FROM `divination_drops`;";

                using (var da = new SQLiteDataAdapter(mapsRunQuery, connection))
                {
                    var runDt = new DataTable("mapsRun");
                    da.Fill(runDt);
                    if (runDt.Rows.Count > 0) listTables.Add(runDt);
                }

                using (var da = new SQLiteDataAdapter(mapQuery, connection))
                {
                    var mapDt = new DataTable("mapTable");
                    da.Fill(mapDt);
                    if (mapDt.Rows.Count > 0) listTables.Add(mapDt);
                }

                using (var da = new SQLiteDataAdapter(uniqueQuery, connection))
                {
                    var uniqueDt = new DataTable("uniqueTable");
                    da.Fill(uniqueDt);
                    if (uniqueDt.Rows.Count > 0) listTables.Add(uniqueDt);
                }

                using (var da = new SQLiteDataAdapter(currencyQuery, connection))
                {
                    var currencyDt = new DataTable("currencyTable");
                    da.Fill(currencyDt);
                    if (currencyDt.Rows.Count > 0) listTables.Add(currencyDt);
                }

                using (var da = new SQLiteDataAdapter(divinationQuery, connection))
                {
                    var divDt = new DataTable("divTable");
                    da.Fill(divDt);
                    if (divDt.Rows.Count > 0) listTables.Add(divDt);
                }
            }
            return listTables;
        } 

        public static void AddDrop(Map newMap, int mapId, int zana = 0, int carto = 0)
        {
            if (newMap == null) return;
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string addQuery = @"INSERT INTO `map_drops` (`map_id`, `rarity`, `level`, `name`, `zana`, `carto`) VALUES 
                                                                  (@mapid, @rarity, @level, @name, @zana, @carto)";
                using (var cmd = new SQLiteCommand(addQuery, connection))
                {
                    cmd.Parameters.AddWithValue("mapid", mapId);
                    cmd.Parameters.AddWithValue("rarity", newMap.Rarity);
                    cmd.Parameters.AddWithValue("level", newMap.Level);
                    cmd.Parameters.AddWithValue("name", newMap.Name);
                    cmd.Parameters.AddWithValue("zana", zana);
                    cmd.Parameters.AddWithValue("carto", carto);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int MapDrops(int mapId, string symbol, string plus)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                var queryMaps = @"SELECT count(d.level) FROM map_drops d JOIN maps m ON d.map_id=m.id WHERE d.map_id=@mapId AND d.level " + symbol + @" m.level ";
                if (plus.Length > 0) queryMaps += plus;
                using (var cmd = new SQLiteCommand(queryMaps, connection))
                {
                    cmd.Parameters.AddWithValue("mapId", mapId);
                    var value = cmd.ExecuteScalar().ToString();
                    return int.Parse(value);
                }
            }
        }

        public static double ExpGained(int id)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string queryGained = @"SELECT exp_after, exp_before, level_before FROM map_experience WHERE map_id=@id";
                using (var cmd = new SQLiteCommand(queryGained, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int level;
                            long expBefore, expAfter;
                            if (!long.TryParse(reader["exp_before"].ToString(), out expBefore) || !long.TryParse(reader["exp_after"].ToString(), out expAfter) || !int.TryParse(reader["level_before"].ToString(), out level)) continue;
                            var value = (expAfter - expBefore);
                            var goal = ExperienceGoal(level);
                            var percentDiff = ((float)value / goal) * 100;
                            return percentDiff;
                        }
                    }
                }
            }
            return 0;
        }

        public static void AddCurrency(int mapId, KeyValuePair<int, string> currency)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertCurrency = @"INSERT OR REPLACE INTO `currency_drops` (`map_id`, `name`, `count`) VALUES (@id, @name, COALESCE((SELECT count FROM currency_drops WHERE name=@name AND map_id=@id), 0) + @count)";
                using (var cmd = new SQLiteCommand(insertCurrency, connection))
                {
                    cmd.Parameters.AddWithValue("id", mapId);
                    cmd.Parameters.AddWithValue("name", currency.Value);
                    cmd.Parameters.AddWithValue("count", currency.Key);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void AddUnique(int mapId, string name)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertCurrency = @"INSERT INTO `unique_drops` (`map_id`, `name`) VALUES (@id, @name)";
                using (var cmd = new SQLiteCommand(insertCurrency, connection))
                {
                    cmd.Parameters.AddWithValue("id", mapId);
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void AddDivination(int mapId, KeyValuePair<int, string> divination)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertCurrency = @"INSERT OR REPLACE INTO `divination_drops` (`map_id`, `name`, `count`) VALUES (@id, @name, COALESCE((SELECT count FROM divination_drops WHERE name=@name AND map_id=@id), 0) + @count)";
                using (var cmd = new SQLiteCommand(insertCurrency, connection))
                {
                    cmd.Parameters.AddWithValue("id", mapId);
                    cmd.Parameters.AddWithValue("name", divination.Value);
                    cmd.Parameters.AddWithValue("count", divination.Key);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<KeyValuePair<int, string>> MapList(int id)
        {
            var mapList = new List<KeyValuePair<int, string>>();
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string mapListQuery = @"SELECT level, name FROM `map_drops` WHERE map_id=@id;";
                using (var cmd = new SQLiteCommand(mapListQuery, connection))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            mapList.Add(new KeyValuePair<int, string>(int.Parse(reader["level"].ToString()), reader["name"].ToString()));
                        }
                    }
                }
            }
            return mapList;
        }

        public static List<string> MapAffixes(int id)
        {
            var affixList = new List<string>();
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string selectAffix = @"SELECT `affix` FROM `affixes` WHERE map_id=@id";
                using (var cmd = new SQLiteCommand(selectAffix, connection))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            affixList.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return affixList;
        }

        public static void FinishMap(int id, Experience exp)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string updateFinish = @"UPDATE maps SET finished_at=@finish WHERE id=@id";
                using (var cmd = new SQLiteCommand(updateFinish, connection))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("finish", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
                //const string updateExperience = @"UPDATE map_experience SET exp_after=@expa, level_after=@levela, percent_after=@percenta WHERE map_id=@id;";
                //using (var cmd = new SQLiteCommand(updateExperience, connection))
                //{
                //    cmd.Parameters.AddWithValue("@expa", exp.CurrentExperience);
                //    cmd.Parameters.AddWithValue("@levela", exp.Level);
                //    cmd.Parameters.AddWithValue("@percenta", exp.Percentage);
                //    cmd.Parameters.AddWithValue("@id", id);
                //    cmd.ExecuteNonQuery();
                //}
            }
        }

        public static bool DeleteMap(int id)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.Parameters.AddWithValue("id", id);

                    cmd.CommandText = @"DELETE FROM `maps` WHERE id=@id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"DELETE FROM `map_drops` WHERE map_id=@id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"DELETE FROM `currency_drops` WHERE map_id=@id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"DELETE FROM `unique_drops` WHERE map_id=@id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"DELETE FROM `affixes` WHERE map_id=@id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"DELETE FROM `map_experience` WHERE map_id=@id";
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
        }

        public static void UpdateNotes(int id, string notes)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string updateNotes = @"UPDATE maps SET notes=@notes WHERE id=@id";
                using (var cmd = new SQLiteCommand(updateNotes, connection))
                {
                    cmd.Parameters.AddWithValue("@notes", notes);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //public void AddCurrency(int id, string currency, int count)
        //{
        //    using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
        //    {
        //        const string insertCurrency = @"INSERT INTO `currency_drops` (`map_id`, `name`, `count`) VALUES (@id, @currency, @count);";
        //        using (var cmd = new SQLiteCommand(insertCurrency, connection))
        //        {
        //            cmd.Parameters.AddWithValue("@id", id);
        //            cmd.Parameters.AddWithValue("@currency", currency);
        //            cmd.Parameters.AddWithValue("@count", count);
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //}

        public static void AddExperience(List<Experience> expList)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertExp = @"INSERT INTO `experience` (level, total_exp, exp_goal) VALUES (@level, @totalExp, @expGoal);";
                var transaction = connection.BeginTransaction();
                using (var cmd = new SQLiteCommand(insertExp, connection))
                {
                    cmd.Parameters.AddWithValue("@level", "");
                    cmd.Parameters.AddWithValue("@totalExp", "");
                    cmd.Parameters.AddWithValue("@expGoal", "");
                    foreach (var item in expList)
                    {
                        InsertExperience(item, cmd);
                    }
                }
                transaction.Commit();
            }
        }

        private static void InsertExperience(Experience exp, SQLiteCommand cmd)
        {
            cmd.Parameters["@level"].Value = exp.Level;
            cmd.Parameters["@totalExp"].Value = exp.CurrentExperience;
            cmd.Parameters["@expGoal"].Value = exp.NextLevelExperience;
            cmd.ExecuteNonQuery();
        }

        public static int ExperienceCount()
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string countExp = @"SELECT count(*) FROM `experience`;";
                using (var cmd = new SQLiteCommand(countExp, connection))
                {
                    int count;
                    var value = cmd.ExecuteScalar();
                    if (value != null && int.TryParse(value.ToString(), out count)) return count;
                }
            }
            return 0;
        }

        public static int ExperienceGoal(int level)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string countExp = @"SELECT exp_goal FROM `experience` WHERE level=@level;";
                using (var cmd = new SQLiteCommand(countExp, connection))
                {
                    cmd.Parameters.AddWithValue("@level", level);
                    int exp;
                    var value = cmd.ExecuteScalar();
                    if (value != null && int.TryParse(value.ToString(), out exp)) return exp;
                }
            }
            return 0;
        }

        public static void AddInformation(List<MapInformation> informationList)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertExp = @"INSERT INTO `map_information` (`name`, `unique`, `zone`, `boss`, `boss_information`, `description`) VALUES (@name, @unique, @zone, " +
                                         @"@boss, @bossinfo, @description);";
                var transaction = connection.BeginTransaction();
                using (var cmd = new SQLiteCommand(insertExp, connection))
                {
                    cmd.Parameters.AddWithValue("@name", "");
                    cmd.Parameters.AddWithValue("@unique", "");
                    cmd.Parameters.AddWithValue("@zone", "");
                    cmd.Parameters.AddWithValue("@boss", "");
                    cmd.Parameters.AddWithValue("@bossinfo", "");
                    cmd.Parameters.AddWithValue("@description", "");
                    foreach (var item in informationList)
                    {
                        cmd.Parameters["@name"].Value = item.Name;
                        cmd.Parameters["@unique"].Value = item.Unique;
                        cmd.Parameters["@zone"].Value = item.Zone;
                        cmd.Parameters["@boss"].Value = item.Boss;
                        cmd.Parameters["@bossinfo"].Value = item.BossDetails;
                        cmd.Parameters["@description"].Value = item.Description;
                        cmd.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
        }

        public static int InformationCount()
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string countQuery = @"SELECT count(*) FROM `map_information`";
                using (var cmd = new SQLiteCommand(countQuery, connection))
                {
                    var value = cmd.ExecuteScalar();
                    int count;
                    if (int.TryParse(value?.ToString(), out count)) return count;
                }
            }
            return 0;
        }

        internal static int CountMapsToday()
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string countMaps = @"SELECT count(*) FROM `maps` WHERE started_at > @dateStart AND started_at < @dateEnd";
                using (var cmd = new SQLiteCommand(countMaps, connection))
                {
                    cmd.Parameters.AddWithValue("@dateStart", DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@dateEnd", DateTime.Now.ToString("yyyy-MM-dd 23:59:59"));
                    var value = cmd.ExecuteScalar();
                    if (value != null) return int.Parse(value.ToString());
                }
            }
            return 0;
        }

        #region Character Names

        public static bool InsertCharacter(string name)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertName = @"INSERT INTO `character_details` (name) VALUES (@name);";
                using (var cmd = new SQLiteCommand(insertName, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public static bool DeleteCharacter(string name)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string deleteName = @"DELETE FROM `character_details` WHERE name=@name;";
                using (var cmd = new SQLiteCommand(deleteName, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public static List<string> CharactersList()
        {
            var characterList = new List<string>();

            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string characterSelect = @"SELECT * FROM `character_details`";
                using (var cmd = new SQLiteCommand(characterSelect, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            characterList.Add(reader["name"].ToString());
                        }
                    }
                }
            }
            return characterList;
        }

        #endregion

        #region Map Information

        internal static bool InsertInformation(List<MapInformation> mapInformation)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string insertInformation = @"INSERT INTO `map_information` (name, unique, boss, boss_information, description, zone) VALUES 
                                            (@name, @unique, @boss, @bossinfo, @desc, @zone);";
                using (var cmd = new SQLiteCommand(insertInformation, connection))
                {
                    cmd.Parameters.AddWithValue("@name", "");
                    cmd.Parameters.AddWithValue("@unique", "");
                    cmd.Parameters.AddWithValue("@boss", "");
                    cmd.Parameters.AddWithValue("@bossinfo", "");
                    cmd.Parameters.AddWithValue("@desc", "");
                    cmd.Parameters.AddWithValue("@zone", "");
                    foreach (var mapInfo in mapInformation)
                    {
                        cmd.Parameters["@name"].Value = mapInfo.Name;
                        cmd.Parameters["@unique"].Value = mapInfo.Unique;
                        cmd.Parameters["@boss"].Value = mapInfo.Boss;
                        cmd.Parameters["@bossinfo"].Value = mapInfo.BossDetails;
                        cmd.Parameters["@desc"].Value = mapInfo.Description;
                        cmd.Parameters["@zone"].Value = mapInfo.Zone;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return true;
        }

        internal static MapInformation MapInformation(string mapName)
        {
            using (var connection = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string selectInformation = @"SELECT * FROM `map_information` WHERE name=@name;";
                using (var cmd = new SQLiteCommand(selectInformation, connection))
                {
                    cmd.Parameters.AddWithValue("@name", mapName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var unique = 0;
                            var mapInformation = new MapInformation
                            {
                                Name = reader["name"].ToString(),
                                Boss = reader["boss"].ToString(),
                                BossDetails = reader["boss_information"].ToString(),
                                Unique = int.TryParse(reader["unique"].ToString(), out unique) ? unique : 0,
                                Description = reader["description"].ToString(),
                                Zone = reader["zone"].ToString()
                            };
                            return mapInformation;
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        internal static ObservableCollection<Error> GetErrors()
        {
            var errorCollection = new ObservableCollection<Error>();
            using (var conn = new SQLiteConnection(Connection).OpenAndReturn())
            {
                const string selectErrors = @"SELECT * FROM `error_log`";
                using (var cmd = new SQLiteCommand(selectErrors, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var newError = new Error
                            {
                                Id = reader.GetInt32(0),
                                Message = reader.GetString(1),
                                InnerException = reader.GetString(2),
                                StackTrace = reader.GetString(3),
                                Time = reader.GetDateTime(4),
                                Environment = reader.GetString(5),
                                Version = reader.GetString(6),
                                Method = reader.GetString(7)
                            };
                            errorCollection.Add(newError);
                        }
                    }
                }
            }
            return errorCollection;
        }

        internal static void WriteError(Exception exError, string method)
        {
            try
            {
                using (var conn = new SQLiteConnection(Connection).OpenAndReturn())
                {
                    const string insertQuery = "INSERT INTO `error_log` (`message`, `inner_exception`, `stack_trace`, `error_time`, `environment`, `version`, `method`) " +
                                               "VALUES (@message, @innerException, @stackTrace, @errorTime, @environment, @user, @version, @method);";

                    conn.Open();

                    using (var cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@message", (exError.Message));
                        cmd.Parameters.AddWithValue("@innerException", exError.InnerException?.ToString() ?? string.Empty);
                        cmd.Parameters.AddWithValue("@stackTrace", (exError.StackTrace ?? string.Empty));
                        cmd.Parameters.AddWithValue("@errorTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@environment", (Environment.OSVersion == null ? string.Empty : Environment.MachineName));
                        cmd.Parameters.AddWithValue("@version", (Assembly.GetEntryAssembly().GetName().Version == null ? string.Empty : Assembly.GetEntryAssembly().GetName().Version.ToString()));
                        cmd.Parameters.AddWithValue("@method", method);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in [WriteError]{0}Message: {1}{0}InnerException: {2}{0}StackTrace{3}", Environment.NewLine, ex.Message, ex.InnerException, ex.StackTrace);
                return;
            }
        }
    }
}