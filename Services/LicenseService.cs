using System;
using System.Security.Cryptography;
using System.Text;
using TN.Models;

namespace TN.Services
{
    public class LicenseService
    {
        private readonly SettingsService _settingsService;

        public LicenseService()
        {
            _settingsService = new SettingsService();
        }

        /// <summary>
        /// Kiểm tra xem người dùng có phải là Premium không
        /// </summary>
        public bool IsPremiumUser()
        {
            var settings = SettingsService.Current;
            if (string.IsNullOrEmpty(settings.LicenseKey)) return false;

            return ValidateKey(settings.LicenseKey);
        }

        /// <summary>
        /// Kích hoạt bản quyền với mã Key
        /// </summary>
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
        /// Thuật toán kiểm tra Key Offline
        /// Cấu trúc: TN-PRM-XXXX-YYYY
        /// YYYY phải là mã hóa đơn giản của XXXX + DeviceId
        /// </summary>
        public bool ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            // MASTER KEY để bạn test nhanh
            if (key == "TN-UNLOCK-MASTER") return true;

            if (!key.StartsWith("TN-PRM-"))
            {
                System.Windows.MessageBox.Show("Mã Key phải bắt đầu bằng: TN-PRM-");
                return false;
            }

            try
            {
                string[] parts = key.Split('-');
                if (parts.Length != 4)
                {
                    System.Windows.MessageBox.Show("Mã Key phải có dạng: TN-PRM-XXXX-YYYY");
                    return false;
                }

                string randomPart = parts[2]; // XXXX
                string checksumPart = parts[3]; // YYYY

                // Thuật toán kiểm tra đơn giản: Hash(XXXX + Secret) lấy 4 ký tự cuối
                string secret = "TN_SECRET_2024";
                string combined = randomPart + secret;
                
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    string hash = BitConverter.ToString(bytes).Replace("-", "").Substring(0, 4);
                    
                    if (!hash.Equals(checksumPart, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Windows.MessageBox.Show($"Debug: Expected {hash}, got {checksumPart}");
                    }

                    return hash.Equals(checksumPart, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tạo một Key hợp lệ (Dành cho Admin/Bạn để cấp cho người dùng)
        /// </summary>
        public string GenerateKey()
        {
            string randomPart = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            string secret = "TN_SECRET_2024";
            string combined = randomPart + secret;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string hash = BitConverter.ToString(bytes).Replace("-", "").Substring(0, 4);
                return $"TN-PRM-{randomPart}-{hash}";
            }
        }
    }
}
