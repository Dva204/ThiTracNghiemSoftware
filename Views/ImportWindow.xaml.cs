using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TN.Models;
using TN.Services;
using Microsoft.Win32;
using System.IO;
using Path = System.IO.Path;
namespace TN.Views
{
    /// <summary>
    /// Interaction logic for ImportWindow.xaml
    /// </summary>
    public partial class ImportWindow : Window
    {
        private readonly StorageService _storageService = new();
        private readonly QuestionParserService _parserService = new();
        private readonly LicenseService _licenseService = new();
        private List<string> _allCategories = new();

        public ImportWindow()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var home = new HomeWindow();
            home.Show();
            this.Close();
        }

        private void LoadCategories()
        {
            _allCategories = _storageService.GetCategories();
            cboCollection.ItemsSource = _allCategories;
            if (_allCategories.Count > 0) cboCollection.SelectedIndex = 0;
        }

        private bool _isAddingNew = false;
        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = !_isAddingNew;
            pnlNewCategory.Visibility = _isAddingNew ? Visibility.Visible : Visibility.Collapsed;
            cboCollection.IsEnabled = !_isAddingNew;
            btnAddCategory.Content = _isAddingNew ? "✖" : "+";
            btnAddCategory.Foreground = _isAddingNew ? Brushes.OrangeRed : new SolidColorBrush(Color.FromRgb(0, 212, 255));
        }

        private void btnOpenEditor_Click(object sender, RoutedEventArgs e)
        {
            if (!_licenseService.IsPremiumUser())
            {
                MessageBox.Show("Trình soạn thảo câu hỏi trực tiếp là tính năng PREMIUM.\nHãy nâng cấp để tự tạo bộ đề của riêng bạn!", 
                                "Tính năng Premium", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editor = new EditorWindow();
            editor.Show();
            this.Close();
        }

        private void btnSelectFile_Click(object sender, RoutedEventArgs e){
            string category = _isAddingNew ? txtNewCategory.Text.Trim() : cboCollection.Text;

            if (string.IsNullOrEmpty(category)) {
                MessageBox.Show("Vui lòng chọn hoặc nhập tên bộ đề!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // KIỂM TRA GIỚI HẠN: Tối đa 3 thư mục con (chỉ cho bản Free)
            bool isPremium = _licenseService.IsPremiumUser();
            var currentCategories = _storageService.GetCategories();

            if (!isPremium) {
                if (_isAddingNew && !currentCategories.Contains(category)) {
                    if (currentCategories.Count >= 3) {
                        MessageBox.Show("Giới hạn FREE: Bạn chỉ được tạo tối đa 3 bộ đề.\nNâng cấp PREMIUM để tạo không giới hạn!", 
                                        "Nâng cấp Premium", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                // KIỂM TRA GIỚI HẠN: Tối đa 5 file mỗi thư mục (chỉ cho bản Free)
                var currentFiles = _storageService.GetBanksByCategory(category);
                if (currentFiles.Count >= 5) {
                    MessageBox.Show($"Giới hạn FREE: Bộ đề '{category}' đã đạt tối đa 5 file.\nNâng cấp PREMIUM để nạp không giới hạn!", 
                                    "Nâng cấp Premium", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            //Mở hộp thoại chọn file của windows
            OpenFileDialog openFileDialog = new OpenFileDialog{
                Filter = "Word Document (*.docx)|*.docx",
                Title = "Chọn file câu hỏi trắc nghiệm"
            };

            if(openFileDialog.ShowDialog() == true){
                try
                {
                    string selectedPath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(selectedPath);
                    
                    //Lưu file vào ổ cứng trong thư mục phân loại
                    string savedPath = _storageService.ImportFileToCategory(selectedPath, fileName, category);
                    //Đánh dấu file này là active
                    _storageService.SetActiveBank(category, fileName);

                    //Đọc câu hỏi từ file vừa lưu
                    List<Question> questions = _parserService.LoadQuestionsFromDocx(savedPath);
                    int actualCount = questions.Count;
                    string limitNote = "";

                    if (!isPremium && actualCount > 60)
                    {
                        limitNote = "\n⚠ (Giới hạn Free: Chỉ sử dụng được 60 câu)";
                    }

                    txtStatus.Text =
                        $"Đã nhập thành công vào bộ đề: [{category}]\n" +
                        $"✔ Số câu hợp lệ: {actualCount} câu {limitNote}\n" +
                        $"File lưu: {fileName}";

                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136));

                    // Reset if new category was added
                    if (_isAddingNew) {
                        _isAddingNew = false;
                        pnlNewCategory.Visibility = Visibility.Collapsed;
                        cboCollection.IsEnabled = true;
                        txtNewCategory.Text = "";
                        btnAddCategory.Content = "+";
                        LoadCategories();
                        cboCollection.SelectedItem = category;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi import file: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Import thất bại! Hãy kiểm tra file.";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 23, 68));
                }
            }
        }

        // ═══════════════════════════════════════
        //         QUẢN LÝ BỘ ĐỀ (SEARCH, RENAME, DELETE)
        // ═══════════════════════════════════════

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch.Text == "Search collections...") return;

            string filter = txtSearch.Text.ToLower().Trim();
            cboCollection.ItemsSource = _allCategories
                .Where(c => c.ToLower().Contains(filter))
                .ToList();
            
            if (cboCollection.Items.Count > 0) cboCollection.SelectedIndex = 0;
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search collections...") txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "Search collections...";
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            if (cboCollection.SelectedItem is string oldName)
            {
                string newName = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Nhập tên mới cho bộ đề '{oldName}':", "Đổi tên bộ đề", oldName);

                if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

                try {
                    _storageService.RenameCategory(oldName, newName);
                    LoadCategories();
                    cboCollection.SelectedItem = newName;
                    MessageBox.Show("Đã đổi tên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (Exception ex) {
                    MessageBox.Show("Lỗi: " + ex.Message);
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_licenseService.IsPremiumUser())
            {
                MessageBox.Show("Xóa bộ đề là tính năng PREMIUM.\nHãy nâng cấp để quản lý kho câu hỏi chuyên nghiệp hơn!", 
                                "Tính năng Premium", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (cboCollection.SelectedItem is string category)
            {
                var result = MessageBox.Show(
                    $"BẠN CÓ CHẮC CHẮN MUỐN XÓA BỘ ĐỀ: [{category}]?\n\nThao tác này sẽ xóa vĩnh viễn tất cả file bên trong!",
                    "XÁC NHẬN XÓA", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _storageService.DeleteCategory(category);
                    LoadCategories();
                    MessageBox.Show("Đã xóa bộ đề thành công.");
                }
            }
        }

        private void cboCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadFiles();
        }

        private void LoadFiles()
        {
            if (cboCollection.SelectedItem is string category)
            {
                var files = _storageService.GetBanksByCategory(category);
                lstFiles.ItemsSource = files;
            }
            else
            {
                lstFiles.ItemsSource = null;
            }
        }

        private void btnDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (cboCollection.SelectedItem is string category && lstFiles.SelectedItem is string fileName)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa file '{fileName}' khỏi bộ đề '{category}'?", 
                    "Xác nhận xóa file", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks", category, fileName);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        LoadFiles();
                        MessageBox.Show("Đã xóa file thành công.");
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một file để xóa!");
            }
        }
    }
}
