namespace RDPManager.Models
{
    public class AppSettings
    {
        public bool StartWithWindows { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool CheckOnlineStatus { get; set; } = true;
        public int PingIntervalSeconds { get; set; } = 30;
    }
}
