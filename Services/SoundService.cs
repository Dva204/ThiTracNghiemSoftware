using System.Media;
using TN.Models;

namespace TN.Services
{
    public class SoundService
    {
        // Phát tiếng tick (mô phỏng đồng hồ)
        public static void PlayTick()
        {
            if (!SettingsService.Current.IsSoundEnabled) return;
            // Phát âm thanh Asterisk mặc định của Windows cho nhẹ
            SystemSounds.Asterisk.Play();
        }

        // Phát tiếng cảnh báo (Hết giờ / Nộp bài)
        public static void PlayAlert()
        {
            if (!SettingsService.Current.IsSoundEnabled) return;
            SystemSounds.Exclamation.Play();
        }
        
        // Phát tiếng thành công
        public static void PlaySuccess()
        {
            if (!SettingsService.Current.IsSoundEnabled) return;
            SystemSounds.Beep.Play();
        }
    }
}
