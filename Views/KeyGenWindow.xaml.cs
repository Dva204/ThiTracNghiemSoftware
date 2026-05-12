using System;
using System.Windows;
using TN.Services;

namespace TN.Views
{
    public partial class KeyGenWindow : Window
    {
        private readonly LicenseService _licenseService = new LicenseService();

        public KeyGenWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            string deviceId = txtDeviceId.Text.Trim();
            if (string.IsNullOrEmpty(deviceId))
            {
                MessageBox.Show("Please enter the Device ID from your customer.");
                return;
            }

            string key = _licenseService.GenerateKeyFromDeviceId(deviceId);
            txtResultKey.Text = key;
        }
    }
}
