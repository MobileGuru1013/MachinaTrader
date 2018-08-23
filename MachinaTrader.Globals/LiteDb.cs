using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace MachinaTrader.Globals
{
    public class LiteDb
    {
        //LiteDB

        private static Dictionary<string, DbManager> _instance = new Dictionary<string, DbManager>();
        public class DbManager
        {
            private LiteDatabase _liteDataBase;
            //private static Manager singleInstance;

            private DbManager(string pluginName, string databaseName)
            {
                _liteDataBase = new LiteDatabase(Global.DataPath + "/" + pluginName + "/" + databaseName + ".db");
            }

            public static DbManager GetInstance(string pluginName, string databaseName)
            {
                if (!_instance.ContainsKey(databaseName))
                {
                    _instance["databaseName"] = new DbManager(pluginName, databaseName);
                }
                return _instance["databaseName"];
            }

            public LiteCollection<T> GetTable<T>(string collectionName = null) where T : new()
            {
                if (collectionName == null)
                {
                    return _liteDataBase.GetCollection<T>(typeof(T).Name);
                }
                return _liteDataBase.GetCollection<T>(collectionName);
            }
        }
    }
}
