using System.Linq;
using System.Windows;
using TN.Services;

namespace TN.Views
{
    public partial class HomeWindow : Window
    {
        private readonly LicenseService _licenseService = new();

        public HomeWindow()
        {
            InitializeComponent();
            
            // Load hệ thống cài đặt dùng chung cho toàn bộ app
            var settingsService = new SettingsService();
            settingsService.LoadSettings();

            CheckLicense();
            LoadDashboardStats();
        }

        private void CheckLicense()
        {
            bool isPremium = _licenseService.IsPremiumUser();
            bdPremiumBadge.Visibility = isPremium ? Visibility.Visible : Visibility.Collapsed;
            btnActivate.Visibility = isPremium ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadDashboardStats()
        {
            var historyService = new TN.Services.HistoryService();
            var records = historyService.LoadHistory();

            if (records.Count > 0)
            {
                txtTotalExams.Text = records.Count.ToString();
                txtTotalQuestions.Text = records.Sum(r => r.TotalQuestions).ToString();
                
                double avgAccuracy = (double)records.Sum(r => r.CorrectAnswers) / records.Sum(r => r.TotalQuestions) * 100;
                txtAccuracy.Text = avgAccuracy.ToString("F0") + "%";

                // Tính streak (đơn giản: số ngày liên tiếp có bản ghi thi)
                // Để đơn giản, ta chỉ hiện số ngày đã học (distinct days)
                int studyDays = records.Select(r => r.ExamDate.Date).Distinct().Count();
                txtStreak.Text = studyDays.ToString();
            }
            else
            {
                txtTotalExams.Text = "0";
                txtTotalQuestions.Text = "0";
                txtAccuracy.Text = "0%";
                txtStreak.Text = "0";
            }
        }

        // Chuyển sang cửa sổ Thi / Luyện Tập
        private void BtnStartExam_Click(object sender, RoutedEventArgs e)
        {
            var examWindow = new ThiTracNghiemWindow();
            examWindow.Show();
            this.Close();
        }

        // Chuyển sang cửa sổ Quản lý bộ đề (Import)
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportWindow();
            importWindow.Show();
            this.Close();
        }

        // Chuyển sang cửa sổ Thống Kê
        private void BtnStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (!_licenseService.IsPremiumUser())
            {
                MessageBox.Show("Tính năng 'Thống kê & Phân tích' chỉ dành cho bản PREMIUM.\nHãy nâng cấp để theo dõi tiến độ của bạn!", 
                                "Tính năng Premium", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var statWindow = new StatisticsWindow();
            statWindow.Show();
            this.Close();
        }

        // Mở cửa sổ cấu hình
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        // Thoát ứng dụng
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            string deviceId = _licenseService.GetDeviceId();
            
            // Hiển thị thông tin hướng dẫn trước
            string infoMsg = $"💎 NÂNG CẤP BẢN PREMIUM\n\n" +
                             $"Mã máy của bạn: {deviceId}\n" +
                             $"Liên hệ Zalo: 0386137558 để nhận mã kích hoạt.\n\n" +
                             $"Nhấn OK để tiếp tục nhập mã kích hoạt.";

            var result = MessageBox.Show(infoMsg, "Kích hoạt Premium", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            
            if (result == MessageBoxResult.OK)
            {
                // Popup nhập Key 
                string key = Microsoft.VisualBasic.Interaction.InputBox(
                    "Nhập mã kích hoạt (License Key) bạn đã nhận được:", 
                    "Kích hoạt Premium", "");

                if (string.IsNullOrEmpty(key)) return;

                if (_licenseService.Activate(key))
                {
                    MessageBox.Show("Chúc mừng! Bạn đã nâng cấp lên bản Premium thành công.", "Thành công", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    CheckLicense();
                }
                else
                {
                    string errorMsg = "Mã kích hoạt không đúng hoặc không dành cho máy này.\n\n" +
                                     $"Mã máy hiện tại: {deviceId}\n" +
                                     "Vui lòng liên hệ Zalo: 0386137558 để được hỗ trợ!";
                                     
                    MessageBox.Show(errorMsg, "Lỗi kích hoạt", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnGuide_Click(object sender, RoutedEventArgs e)
        {
            var guide = new GuideWindow();
            guide.ShowDialog();
        }
    }
}
