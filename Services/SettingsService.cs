using System;
using System.IO;
using System.Text.Json;
using TN.Models;

namespace TN.Services
{
    public class SettingsService
    {
        private readonly string _folderPath;
        private readonly string _filePath;

        // Lưu bản duy nhất để chạy toàn app (Singleton)
        public static AppSettings Current { get; private set; } = new AppSettings();

        public SettingsService()
        {
            _folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Settings");
            _filePath = Path.Combine(_folderPath, "settings.json");
        }

        public void LoadSettings()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) Current = settings;
                }
                catch { /* Ignore */ }
            }
        }

        public void SaveSettings()
        {
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Current, options);
            File.WriteAllText(_filePath, json);
        }
    }
}
