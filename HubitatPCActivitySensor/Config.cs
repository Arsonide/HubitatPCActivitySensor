namespace HubitatPCActivitySensor
{
    public class Config
    {
        public string HubIp { get; set; }
        public string AppId { get; set; }
        public string DeviceId { get; set; }
        public string AccessToken { get; set; }
        public int PulseIntervalSeconds { get; set; } = 120;
        public int IdleThresholdSeconds { get; set; } = 60;
        public int MaxAllowedTickDurationSeconds { get; set; } = 300;
        public int TickTimeSeconds { get; set; } = 1;
        public bool EnableLogging { get; set; } = true;
    }
}