using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using TN.Models;
using TN.Services;

using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TN.Views
{
    public partial class EditorWindow : Window
    {
        private readonly StorageService _storageService = new();
        private ExamBank _currentBank = new();
        private string _currentFilePath = "";
        private string? _selectedImagePath = null;

        public EditorWindow()
        {
            InitializeComponent();
            LoadCollections();
        }

        private void LoadCollections()
        {
            cboCollections.ItemsSource = _storageService.GetCategories();
            if (cboCollections.Items.Count > 0) cboCollections.SelectedIndex = 0;
        }

        private void cboCollections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadBanksForCategory();
        }

        private void LoadBanksForCategory()
        {
            if (cboCollections.SelectedItem is string category)
            {
                var files = _storageService.GetBanksByCategory(category);
                var jsonFiles = files.Where(f => f.EndsWith(".json")).ToList();
                
                var displayList = new List<BankDisplayItem>();
                foreach (var file in jsonFiles)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks", category, file);
                    string title = Path.GetFileNameWithoutExtension(file);
                    try {
                        string json = File.ReadAllText(path);
                        var bankData = JsonSerializer.Deserialize<ExamBank>(json);
                        if (bankData != null) title = bankData.Title;
                    } catch { }
                    displayList.Add(new BankDisplayItem { FileName = file, Title = title });
                }

                cboBanks.ItemsSource = displayList;
                cboBanks.DisplayMemberPath = "Title";
                cboBanks.SelectedValuePath = "FileName";
                
                if (displayList.Count > 0)
                {
                    cboBanks.SelectedIndex = 0;
                }
                else
                {
                    _currentFilePath = "";
                    txtBankTitle.Text = "";
                    _currentBank = new ExamBank { Title = "Bộ đề mới (" + category + ")" };
                    lstQuestions.ItemsSource = null;
                    pnlForm.Visibility = Visibility.Hidden;
                }
            }
        }

        private void cboBanks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCollections.SelectedItem is string category && cboBanks.SelectedValue is string jsonFile)
            {
                _currentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks", category, jsonFile);
                LoadBank();
            }
        }

        private void btnNewBank_Click(object sender, RoutedEventArgs e)
        {
            if (cboCollections.SelectedItem is string category)
            {
                // Tạo tên file ngẫu nhiên theo tick để không trùng
                string fileName = "bank_" + DateTime.Now.Ticks + ".json";
                _currentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks", category, fileName);
                
                _currentBank = new ExamBank { Title = "Bộ đề tự soạn mới" };
                txtBankTitle.Text = _currentBank.Title;
                lstQuestions.ItemsSource = null;
                pnlForm.Visibility = Visibility.Hidden;
                
                cboBanks.SelectedIndex = -1;
                MessageBox.Show("Hãy đặt tên hiển thị và bắt đầu thêm câu hỏi!", "Tạo bộ đề mới");
            }
        }

        private void LoadBank()
        {
            try
            {
                if (File.Exists(_currentFilePath))
                {
                    string json = File.ReadAllText(_currentFilePath);
                    try 
                    {
                        // Thử load theo chuẩn mới (ExamBank)
                        _currentBank = JsonSerializer.Deserialize<ExamBank>(json) ?? new ExamBank();
                    }
                    catch 
                    {
                        // Nếu lỗi, thử load theo chuẩn cũ (List<Question>) rồi convert sang chuẩn mới
                        var oldFormat = JsonSerializer.Deserialize<List<Question>>(json);
                        _currentBank = new ExamBank 
                        { 
                            Title = Path.GetFileNameWithoutExtension(_currentFilePath),
                            Questions = oldFormat ?? new List<Question>() 
                        };
                    }
                    
                    txtBankTitle.Text = _currentBank.Title;
                    lstQuestions.ItemsSource = null;
                    lstQuestions.ItemsSource = _currentBank.Questions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi nạp bộ đề: " + ex.Message);
            }
        }

        private void lstQuestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstQuestions.SelectedItem is Question q)
            {
                pnlForm.Visibility = Visibility.Visible;
                txtQuestion.Text = q.Text;
                txtA.Text = q.Options.Count > 0 ? q.Options[0] : "";
                txtB.Text = q.Options.Count > 1 ? q.Options[1] : "";
                txtC.Text = q.Options.Count > 2 ? q.Options[2] : "";
                txtD.Text = q.Options.Count > 3 ? q.Options[3] : "";

                rbA.IsChecked = q.CorrectIndex == 1;
                rbB.IsChecked = q.CorrectIndex == 2;
                rbC.IsChecked = q.CorrectIndex == 3;
                rbD.IsChecked = q.CorrectIndex == 4;

                ShowImagePreview(q.ImagePath);
            }
        }

        private void ShowImagePreview(string? imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                string fullPath = imagePath;
                if (!Path.IsPathRooted(fullPath) && !string.IsNullOrEmpty(_currentFilePath))
                {
                    fullPath = Path.Combine(Path.GetDirectoryName(_currentFilePath)!, fullPath);
                }

                if (File.Exists(fullPath))
                {
                    try
                    {
                        _selectedImagePath = fullPath;
                        txtImagePath.Text = Path.GetFileName(fullPath);
                        
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        
                        imgPreview.Source = bitmap;
                        pnlImagePreview.Visibility = Visibility.Visible;
                        return;
                    }
                    catch { }
                }
            }
            ClearImageUI();
        }

        private void ClearImageUI()
        {
            _selectedImagePath = null;
            txtImagePath.Text = "No image selected";
            imgPreview.Source = null;
            pnlImagePreview.Visibility = Visibility.Collapsed;
        }

        private void btnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedImagePath = dialog.FileName;
                txtImagePath.Text = Path.GetFileName(_selectedImagePath);
                
                var bitmap = new BitmapImage(new Uri(_selectedImagePath));
                imgPreview.Source = bitmap;
                pnlImagePreview.Visibility = Visibility.Visible;
            }
        }

        private void btnClearImage_Click(object sender, RoutedEventArgs e)
        {
            ClearImageUI();
        }

        private void btnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            pnlForm.Visibility = Visibility.Visible;
            txtQuestion.Text = ""; // Để trống hoàn toàn để nhập luôn
            txtA.Text = ""; txtB.Text = ""; txtC.Text = ""; txtD.Text = "";
            rbA.IsChecked = true;
            lstQuestions.SelectedIndex = -1;
            ClearImageUI();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                MessageBox.Show("Vui lòng chọn một bộ đề hiện có hoặc nhấn nút (+) để tạo bộ đề mới trước khi lưu câu hỏi!", "Thông báo");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtQuestion.Text)) return;

            // Cập nhật tên bộ đề từ textbox
            _currentBank.Title = txtBankTitle.Text;

            var q = lstQuestions.SelectedItem as Question;
            bool isNew = (q == null);

            if (isNew) q = new Question();

            q.Text = txtQuestion.Text;
            q.Options = new List<string> { txtA.Text, txtB.Text, txtC.Text, txtD.Text };
            
            if (rbA.IsChecked == true) q.CorrectIndex = 1;
            else if (rbB.IsChecked == true) q.CorrectIndex = 2;
            else if (rbC.IsChecked == true) q.CorrectIndex = 3;
            else if (rbD.IsChecked == true) q.CorrectIndex = 4;

            // Xử lý lưu ảnh
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                try
                {
                    string bankDir = Path.GetDirectoryName(_currentFilePath)!;
                    string mediaDir = Path.Combine(bankDir, "Media");
                    if (!Directory.Exists(mediaDir)) Directory.CreateDirectory(mediaDir);

                    string fileName = Path.GetFileName(_selectedImagePath);
                    string targetPath = Path.Combine(mediaDir, fileName);

                    if (Path.GetFullPath(_selectedImagePath) != Path.GetFullPath(targetPath))
                    {
                        File.Copy(_selectedImagePath, targetPath, true);
                    }
                    q.ImagePath = Path.Combine("Media", fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể lưu ảnh: " + ex.Message);
                    q.ImagePath = null;
                }
            }
            else
            {
                q.ImagePath = null;
            }

            if (isNew) _currentBank.Questions.Add(q);

            SaveToFile();
            
            // Refresh danh sách bank để hiển thị Title mới nhất
            string category = cboCollections.SelectedItem as string;
            string currentFileName = Path.GetFileName(_currentFilePath);
            
            LoadBanksForCategory();
            cboBanks.SelectedValue = currentFileName;

            LoadBank();
            
            // Tự động chuẩn bị form cho câu hỏi mới tiếp theo
            btnAddQuestion_Click(null, null);
        }

        private void btnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (lstQuestions.SelectedItem is Question q)
            {
                _currentBank.Questions.Remove(q);
                SaveToFile();
                LoadBank();
                pnlForm.Visibility = Visibility.Hidden;
            }
        }

        private void SaveToFile()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath)) return;

                string dir = Path.GetDirectoryName(_currentFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                _currentBank.Title = txtBankTitle.Text; // Đảm bảo Title mới nhất được lưu

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_currentBank, options);
                File.WriteAllText(_currentFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var home = new HomeWindow();
            home.Show();
            this.Close();
        }

        private void btnBackToImport_Click(object sender, RoutedEventArgs e)
        {
            // Lưu lần cuối trước khi thoát
            SaveToFile();
            
            var import = new ImportWindow();
            import.Show();
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private class BankDisplayItem
        {
            public string FileName { get; set; } = "";
            public string Title { get; set; } = "";
        }
    }
}
