using EasyConfig;

namespace MySQLClient
{
    [ConfigFile("Connection")]
    public sealed class MySQLClientConfig : IConfig
    {
        public string Server { get; set; } = "localhost";
        public uint Port { get; set; } = 3306;
        public string UserID { get; set; } = "MySQLClientPlugin";
        public string Password { get; set; } = "top_secret-123**!";
        public string Database { get; set; } = "MyLocalNwNServerDatabase";
        public bool SSL { get; set; } = true;
        public string CharacterSet { get; set; } = "utf8mb4";

        public void Coerce(){}

        public bool IsValid(out string? error)
        {
            error = "";

            if (Port > 65535)
                error += "Port is out of range\n";

            if (string.IsNullOrEmpty(Server))
                error += "Missing Host";

            if (string.IsNullOrEmpty(UserID))
                error += "Missing UserID";

            if (string.IsNullOrEmpty(Database))
                error += "Missing Database";

            error = error == string.Empty ? null : error;

            return error == null;
        }
    }
    
}