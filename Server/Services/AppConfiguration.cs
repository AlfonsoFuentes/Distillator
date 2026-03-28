namespace Server.Services
{
    public class AppConfiguration
    {
        public string Secret { get; set; } = string.Empty;

        public bool BehindSSLProxy { get; set; }

        public string ProxyIP { get; set; } = string.Empty;

        public string ApplicationUrl { get; set; } = string.Empty;
    }
}
