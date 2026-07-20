namespace BIA.Entity.StaticClass
{
    public sealed class HttpClientSettings
    {
        public int TimeoutSeconds { get; set; } = 300;
        public int ConnectTimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
    }
}
