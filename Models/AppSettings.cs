namespace TN.Models
{
    public class AppSettings
    {
        public bool IsSoundEnabled { get; set; } = true;
        public string Theme { get; set; } = "Light"; // "Light" hoặc "Dark"
        public int FontSizeOffset { get; set; } = 0;
        public string LicenseKey { get; set; } = "";
        public bool IsPremium { get; set; } = false;
        public string DeviceId { get; set; } = "";
    }
}
