using System.Windows;
using TN.Services;
using TN.Models;
using System;

namespace TN.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly LicenseService _licenseService = new LicenseService();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Binding dữ liệu từ bộ nhớ dùng chung Current
            chkSound.IsChecked = SettingsService.Current.IsSoundEnabled;

            // Load bản quyền
            txtDeviceId.Text = _licenseService.GetDeviceId();
            txtLicenseKey.Text = SettingsService.Current.LicenseKey;
            UpdateLicenseUI();
        }

        private void UpdateLicenseUI()
        {
            bool isPremium = _licenseService.IsPremiumUser();
            if (isPremium)
            {
                txtStatus.Text = "PREMIUM ACTIVATED";
                pnlStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 250, 229));
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(5, 150, 105));
                btnActivate.IsEnabled = false;
                btnActivate.Content = "ĐÃ KÍCH HOẠT";
                txtLicenseKey.IsReadOnly = true;
            }
            else
            {
                txtStatus.Text = "FREE VERSION";
                pnlStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 149, 149));
                txtStatus.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtDeviceId.Text);
            MessageBox.Show("Đã copy mã máy vào bộ nhớ tạm!", "Thông báo");
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            string key = txtLicenseKey.Text.Trim();
            if (_licenseService.Activate(key))
            {
                MessageBox.Show("Chúc mừng! Bạn đã nâng cấp lên bản PREMIUM thành công.", "Kích hoạt thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateLicenseUI();
            }
            else
            {
                MessageBox.Show("Mã kích hoạt không hợp lệ cho máy này. Vui lòng kiểm tra lại!", "Lỗi kích hoạt", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Current.IsSoundEnabled = chkSound.IsChecked ?? true;
            var svc = new SettingsService();
            svc.SaveSettings();
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
