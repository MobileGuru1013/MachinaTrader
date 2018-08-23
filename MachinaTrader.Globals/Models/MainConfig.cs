using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Globals.Models
{
    public class MainConfig
    {
        public SystemOptions SystemOptions = new SystemOptions();
        public TradeOptions TradeOptions = new TradeOptions();
        public TelegramNotificationOptions TelegramOptions = new TelegramNotificationOptions();
        public List<ExchangeOptions> ExchangeOptions = new List<ExchangeOptions> { };
        public DisplayOptions DisplayOptions = new DisplayOptions();
    }

    public class SystemOptions
    {
        public string Database { get; set; } = "MongoDB";
        public MongoDbOptions MongoDbOptions = new MongoDbOptions();

        // Frontend stuff
        public int WebPort { get; set; } = 5000;
        public string RsaPrivateKey { get; set; } = "";
        public string DefaultUserName { get; set; } = "admin";
        public string DefaultUserEmail { get; set; } = "admin@localhost";
        public string DefaultUserPassword { get; set; } = "admin";
        public string Theme { get; set; } = "dark";
    }

    public class MongoDbOptions
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 27017;
        public string Username { get; set; } = null;
        public string Password { get; set; } = null;
    }

}
