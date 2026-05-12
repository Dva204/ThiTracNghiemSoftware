using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using TN.Models;

namespace TN.Services
{
    public class LicenseService
    {
        private readonly SettingsService _settingsService;
        private const string SECRET_KEY = "dovanan250820004";
        private const string MASTER_KEY = "TN-MASTER-FULL-ACCESS"; // Key toàn năng

        public LicenseService()
        {
            _settingsService = new SettingsService();
        }

        public bool IsPremiumUser()
        {
            var settings = SettingsService.Current;
            if (string.IsNullOrEmpty(settings.LicenseKey)) return false;
            return ValidateKey(settings.LicenseKey);
        }

        public bool Activate(string key)
        {
            if (ValidateKey(key))
            {
                var settings = SettingsService.Current;
                settings.LicenseKey = key;
                settings.IsPremium = true;
                _settingsService.SaveSettings();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Lấy mã định danh duy nhất của máy tính (Hardware ID)
        /// </summary>
        public string GetDeviceId()
        {
            try
            {
                // Kết hợp các thông tin cố định của máy
                string raw = Environment.MachineName + 
                             Environment.ProcessorCount + 
                             Environment.UserName + 
                             System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
                
                return GetHash(raw).Substring(0, 8).ToUpper();
            }
            catch
            {
                return "ERR-DEVICE-ID";
            }
        }

        public bool ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            // 1. Kiểm tra Key Toàn Năng (Master Key)
            if (key.Trim() == MASTER_KEY) return true;

            // 2. Kiểm tra định dạng TN-PRM-XXXX-YYYY
            string[] parts = key.Split('-');
            if (parts.Length != 4) return false;

            string randomPart = parts[2]; // XXXX
            string checksumPart = parts[3]; // YYYY

            // 3. Logic kiểm tra HWID gắn trong Key
            string deviceId = GetDeviceId();
            string expectedHash = GenerateChecksum(deviceId, randomPart);

            return checksumPart.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        public string GenerateKeyFromDeviceId(string deviceId)
        {
            string randomPart = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            string checksum = GenerateChecksum(deviceId, randomPart);
            return $"TN-PRM-{randomPart}-{checksum}";
        }

        private string GenerateChecksum(string deviceId, string randomPart)
        {
            string combined = deviceId + randomPart + SECRET_KEY;
            return GetHash(combined).Substring(0, 6).ToUpper();
        }

        private string GetHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }
}
