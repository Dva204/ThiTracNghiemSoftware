using System.Windows;
using TN.Services;
using TN.Models;

namespace TN.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Binding dữ liệu từ bộ nhớ dùng chung Current
            chkSound.IsChecked = SettingsService.Current.IsSoundEnabled;
            chkDarkTheme.IsChecked = SettingsService.Current.Theme == "Dark";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Lưu dữ liệu ngược lại vào Current
            SettingsService.Current.IsSoundEnabled = chkSound.IsChecked ?? true;
            SettingsService.Current.Theme = (chkDarkTheme.IsChecked == true) ? "Dark" : "Light";

            // Gọi trigger save file
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
