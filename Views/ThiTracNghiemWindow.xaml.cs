using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using TN.Models;
using TN.Services;
using TN.Views;
using Path = System.IO.Path;
namespace TN
{
    /// <summary>
    /// Interaction logic for ThiTracNghiemWindow.xaml
    /// </summary>
    public partial class ThiTracNghiemWindow : Window
    {
        // ═══════════════════════════════════════
        //              DỮ LIỆU
        // ═══════════════════════════════════════
        private List<Question> _allQuestions = new();
        private readonly ExamService _examService = new();
        private readonly HistoryService _historyService = new();
        private readonly LicenseService _licenseService = new();
        private readonly QuestionParserService _parserService = new();
        private readonly StorageService _storageService = new();
        private readonly ResultService _resultService = new();

        // ═══════════════════════════════════════
        //              ĐỒNG HỒ
        // ═══════════════════════════════════════
        private DispatcherTimer _timer;
        private int _remainingSeconds;

        //              KHỞI TẠO
        // ═══════════════════════════════════════
        public ThiTracNghiemWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 1. Load danh sách môn học (thư mục con)
            var categories = _storageService.GetCategories();
            cboCategories.ItemsSource = categories;

            // 2. Chọn lại môn học và file cuối cùng (nếu có)
            string lastCategory = _storageService.GetActiveCategory();
            string lastFile = _storageService.GetActiveFileName();

            if (!string.IsNullOrEmpty(lastCategory) && categories.Contains(lastCategory))
            {
                cboCategories.SelectedItem = lastCategory;
                // Khi gán SelectedItem, SelectionChanged sẽ tự chạy để load cboQuestionBanks
                if (!string.IsNullOrEmpty(lastFile))
                {
                    var banks = _storageService.GetBanksByCategory(lastCategory);
                    if (banks.Contains(lastFile))
                    {
                        cboQuestionBanks.SelectedValue = lastFile;
                    }
                }
            }
            else if (categories.Count > 0)
            {
                cboCategories.SelectedIndex = 0;
            }
        }

        public void cboCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCategories.SelectedItem is string category)
            {
                var banks = _storageService.GetBanksByCategory(category);
                var displayList = new List<BankDisplayItem>();

                foreach (var file in banks)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks", category, file);
                    string displayName = file;

                    if (file.EndsWith(".json"))
                    {
                        try {
                            string json = File.ReadAllText(path);
                            try {
                                var bankData = JsonSerializer.Deserialize<ExamBank>(json);
                                displayName = "★ " + (bankData?.Title ?? file);
                            } catch {
                                // Fallback cho file cũ
                                displayName = "★ " + Path.GetFileNameWithoutExtension(file);
                            }
                        } catch { displayName = "⚠ " + file; }
                    }
                    else if (file.EndsWith(".docx"))
                    {
                        displayName = "📄 " + Path.GetFileNameWithoutExtension(file);
                    }

                    displayList.Add(new BankDisplayItem { DisplayName = displayName, FileName = file });
                }

                cboQuestionBanks.ItemsSource = displayList;
                cboQuestionBanks.DisplayMemberPath = "DisplayName";
                cboQuestionBanks.SelectedValuePath = "FileName";

                if (displayList.Count > 0) cboQuestionBanks.SelectedIndex = 0;
            }
        }

        public void cboQuestionBanks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCategories.SelectedItem is string category && cboQuestionBanks.SelectedValue is string selectedFile)
            {
                // Đánh dấu file này là file đang active
                _storageService.SetActiveBank(category, selectedFile);

                // Tải lại câu hỏi của file này để biết được tổng số câu
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks", category, selectedFile);
                try
                {
                    if (selectedFile.ToLower().EndsWith(".json"))
                    {
                        string json = File.ReadAllText(path);
                        try {
                            var bankData = JsonSerializer.Deserialize<ExamBank>(json);
                            _allQuestions = bankData?.Questions ?? new List<Question>();
                        } catch {
                            // Fallback cho file cũ
                            _allQuestions = JsonSerializer.Deserialize<List<Question>>(json) ?? new List<Question>();
                        }
                    }
                    else
                    {
                        _allQuestions = _parserService.LoadQuestionsFromDocx(path);
                        
                        // GIỚI HẠN BẢN FREE: Chỉ đọc tối đa 60 câu từ file Word
                        if (!_licenseService.IsPremiumUser() && _allQuestions.Count > 60)
                        {
                            _allQuestions = _allQuestions.Take(60).ToList();
                            MessageBox.Show("Phiên bản FREE chỉ hỗ trợ đọc tối đa 60 câu hỏi từ file Word.\n\nHãy nâng cấp bản quyền PREMIUM để mở khóa giới hạn này!", 
                                            "Giới hạn phiên bản FREE", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    ValidateSetup(); 
                    if (txtRangeInfo != null) txtRangeInfo.Text = $"Available: {_allQuestions.Count}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tải file: " + ex.Message);
                }
            }
        }

        public class BankDisplayItem
        {
            public string DisplayName { get; set; }
            public string FileName { get; set; }
        }

        //         CẬP NHẬT NHÃN KHO CÂU
        // ═══════════════════════════════════════
        private void UpdateCountLabels()
        {
            // Do nothing since we removed specific count labels for difficulty
        }

        // ═══════════════════════════════════════
        //         SỰ KIỆN TEXTBOX
        // ═══════════════════════════════════════
        private void OnSettingChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ValidateSetup();
        }

        private bool ValidateSetup()
        {
            if (!int.TryParse(txtTotalQuestions?.Text, out int total) || total < 1)
            {
                ShowWarning("Số câu hỏi không hợp lệ.");
                return false;
            }

            int avail = _allQuestions.Count;

            if (total > avail)
            {
                ShowWarning($"Yêu cầu {total} câu nhưng chỉ có {avail} câu trong kho.");
                return false;
            }

            HideWarning();
            return true;
        }

        private void ShowWarning(string msg)
        {
            if (pnlWarning == null) return;
            txtWarning.Text = msg;
            pnlWarning.Visibility = Visibility.Visible;
            if (btnStart != null) btnStart.IsEnabled = false;
        }

        private void HideWarning()
        {
            if (pnlWarning == null) return;
            pnlWarning.Visibility = Visibility.Collapsed;
            if (btnStart != null) btnStart.IsEnabled = true;
        }

        // ═══════════════════════════════════════
        //         BẮT ĐẦU THI
        // ═══════════════════════════════════════
        public void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSetup()) return;

            int total = int.Parse(txtTotalQuestions.Text);
            _examService.StartExam(_allQuestions, total);
            // Khởi tạo đồng hồ
            int timeLimitMin = int.TryParse(txtTimeLimit.Text, out int t) ? t : 60;
            _remainingSeconds = timeLimitMin * 60;
            StartTimer();

            // Vẽ bảng điều hướng
            BuildNavButtons();

            // Chuyển màn hình
            pnlSetup.Visibility = Visibility.Collapsed;
            pnlExam.Visibility = Visibility.Visible;
            pnlResult.Visibility = Visibility.Collapsed;
            pnlExamInfo.Visibility = Visibility.Visible;
            pnlTimer.Visibility = Visibility.Visible;

            ShowQuestion(_examService.CurrentIndex);
        }

        public void btnPractice_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSetup()) return;

            int total = int.Parse(txtTotalQuestions.Text);

            // Khởi động hệ thống với cờ Practice
            _examService.StartExam(_allQuestions, total, SessionMode.Practice);

            // Thiết lập đồng hồ giả vì luyện tập không tính giờ
            _remainingSeconds = 999999;
            txtTimer.Text = "Không giới hạn";

            // Hiển thị UI
            BuildNavButtons();
            UpdateProgressBar();

            pnlSetup.Visibility = Visibility.Collapsed;
            pnlExam.Visibility = Visibility.Visible;

            pnlExamInfo.Visibility = Visibility.Visible;
            // Ở chế độ luyện tập, ta có thể tắt khung đồng hồ đi cho bớt áp lực
            pnlTimer.Visibility = Visibility.Collapsed;

            ShowQuestion(_examService.CurrentIndex);
        }

        // ═══════════════════════════════════════
        //         ĐỒNG HỒ ĐẾM NGƯỢC
        // ═══════════════════════════════════════
        private void StartTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            UpdateTimerDisplay();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _remainingSeconds--;

            // Âm thanh cảnh báo 5 giây cuối
            if (_remainingSeconds > 0 && _remainingSeconds <= 5)
            {
                TN.Services.SoundService.PlayTick();
            }

            if (_remainingSeconds % 10 == 0) {
                _examService.AutoSaveSession(_remainingSeconds);
            }
            UpdateTimerDisplay();

            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                MessageBox.Show("⏱ Hết giờ! Bài thi sẽ được nộp tự động.",
                                "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowResult();
            }
            else if (_remainingSeconds <= 300) // 5 phút cuối → đổi màu đỏ
            {
                txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 23, 68));
            }
        }

        private void UpdateTimerDisplay()
        {
            int m = _remainingSeconds / 60;
            int s = _remainingSeconds % 60;
            txtTimer.Text = $"{m:D2}:{s:D2}";
        }

        // ═══════════════════════════════════════
        //         HIỂN THỊ CÂU HỎI
        // ═══════════════════════════════════════
        private void ShowQuestion(int index)
        {
            if (index < 0 || index >= _examService.ExamQuestions.Count) return;

            _examService.CurrentIndex = index;
            var q = _examService.ExamQuestions[index];

            // (Đã bỏ badge độ khó)

            txtCurrentQIndex.Text = $"QUESTION {index + 1:D2}";
            txtQuestion.Text = q.Text;

            // Gán đáp án vào Content để hiển thị
            rbA.Content = q.Options.Count > 0 ? q.Options[0] : "";
            rbB.Content = q.Options.Count > 1 ? q.Options[1] : "";
            rbC.Content = q.Options.Count > 2 ? q.Options[2] : "";
            rbD.Content = q.Options.Count > 3 ? q.Options[3] : "";

            // Reset chọn trước khi set
            rbA.IsChecked = false;
            rbB.IsChecked = false;
            rbC.IsChecked = false;
            rbD.IsChecked = false;

            var defaultColor = new SolidColorBrush(Color.FromRgb(26, 34, 51));
            rbA.Foreground = defaultColor;
            rbB.Foreground = defaultColor;
            rbC.Foreground = defaultColor;
            rbD.Foreground = defaultColor;

            // Xóa màu nền custom
            rbA.ClearValue(BackgroundProperty); rbA.ClearValue(BorderBrushProperty);
            rbB.ClearValue(BackgroundProperty); rbB.ClearValue(BorderBrushProperty);
            rbC.ClearValue(BackgroundProperty); rbC.ClearValue(BorderBrushProperty);
            rbD.ClearValue(BackgroundProperty); rbD.ClearValue(BorderBrushProperty);

            // Khôi phục lựa chọn đã lưu
            if (_examService.UserAnswers.TryGetValue(index, out int saved))
            {
                var rbs = new[] { rbA, rbB, rbC, rbD };
                rbs[saved - 1].IsChecked = true;
            }

            // Cập nhật header
            txtProgress.Text = $"{(double)(index + 1) / _examService.ExamQuestions.Count * 100:F0}%";
            // txtCount updated via UpdateProgressBar below

            // Cập nhật progress bar
            UpdateProgressBar();

            // Highlight nút điều hướng đang xem
            HighlightNavButton(index);

            // Nút prev
            btnPrev.IsEnabled = index > 0;
            btnPrev.Opacity = index > 0 ? 1.0 : 0.4;

            UpdateFlagButtonUI();
        }

        private void UpdateFlagButtonUI()
        {
            if (_examService.FlaggedQuestions.Contains(_examService.CurrentIndex))
            {
                btnFlag.Content = "🚩 FLAGGED";
                btnFlag.Foreground = (SolidColorBrush)FindResource("DangerRed");
            }
            else
            {
                btnFlag.Content = "⚑ FLAG FOR REVIEW";
                btnFlag.Foreground = (SolidColorBrush)FindResource("WarningOrange");
            }
        }

        // ═══════════════════════════════════════
        //         LƯU ĐÁP ÁN
        // ═══════════════════════════════════════
        public void OnAnswerChecked(object sender, RoutedEventArgs e)
        {
            int ans = 0;
            if (sender == rbA) ans = 1;
            else if (sender == rbB) ans = 2;
            else if (sender == rbC) ans = 3;
            else if (sender == rbD) ans = 4;

            if (ans > 0)
            {
                _examService.UserAnswers[_examService.CurrentIndex] = ans;
                MarkNavButtonAnswered(_examService.CurrentIndex);
                UpdateProgressBar();

                // Kiểm tra đúng sai ở Practice Mode
                if (_examService.CurrentMode == SessionMode.Practice) {
                    var q = _examService.ExamQuestions[_examService.CurrentIndex];
                    // Sai thì bôi đỏ đáp án vừa chọn (nền nhạt đỏ, viền đỏ)
                    SolidColorBrush redBg = new SolidColorBrush(Color.FromRgb(255, 230, 230));
                    if (ans == 1 && q.CorrectIndex != 1) { rbA.Background = redBg; rbA.BorderBrush = Brushes.Red; }
                    if (ans == 2 && q.CorrectIndex != 2) { rbB.Background = redBg; rbB.BorderBrush = Brushes.Red; }
                    if (ans == 3 && q.CorrectIndex != 3) { rbC.Background = redBg; rbC.BorderBrush = Brushes.Red; }
                    if (ans == 4 && q.CorrectIndex != 4) { rbD.Background = redBg; rbD.BorderBrush = Brushes.Red; }

                    // Luôn bôi xanh đáp án đúng (nền nhạt xanh ngọc, viền xanh ngọc)
                    SolidColorBrush greenBg = new SolidColorBrush(Color.FromRgb(230, 255, 238));
                    if (q.CorrectIndex == 1) { rbA.Background = greenBg; rbA.BorderBrush = Brushes.MediumSeaGreen; }
                    if (q.CorrectIndex == 2) { rbB.Background = greenBg; rbB.BorderBrush = Brushes.MediumSeaGreen; }
                    if (q.CorrectIndex == 3) { rbC.Background = greenBg; rbC.BorderBrush = Brushes.MediumSeaGreen; }
                    if (q.CorrectIndex == 4) { rbD.Background = greenBg; rbD.BorderBrush = Brushes.MediumSeaGreen; }
                }
            }
        }

        // ═══════════════════════════════════════
        //         THANH TIẾN ĐỘ
        // ═══════════════════════════════════════
        private void UpdateProgressBar()
        {
            int total = _examService.ExamQuestions.Count;
            int answered = _examService.UserAnswers.Count;

            txtCount.Text = $"{answered} / {total} Questions";
            
            double ratio = total > 0 ? (double)answered / total : 0;
            pbProgress.Value = ratio * 100;
            txtProgress.Text = $"{(ratio * 100):F0}% Complete";
        }

        // ═══════════════════════════════════════
        //         BẢNG ĐIỀU HƯỚNG
        // ═══════════════════════════════════════
        private void BuildNavButtons()
        {
            spPaging.Children.Clear();
            for (int i = 0; i < _examService.ExamQuestions.Count; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Style = FindResource("QuestionPillStyle") as Style,
                    Tag = i
                };
                btn.Click += (s, e) => ShowQuestion(idx);
                spPaging.Children.Add(btn);
            }
        }

        private void HighlightNavButton(int index)
        {
            for (int i = 0; i < spPaging.Children.Count; i++)
            {
                if (spPaging.Children[i] is not Button btn) continue;
                if (btn.Template == null) continue;

                var border = btn.Template.FindName("bd", btn) as Border;
                if (border == null) continue;

                bool isCurrent = i == index;
                bool isAnswered = _examService.UserAnswers.ContainsKey(i);
                bool isFlagged = _examService.FlaggedQuestions.Contains(i);

                if (isCurrent)
                {
                    border.Background = (SolidColorBrush)FindResource("AccentBlue");
                    btn.Foreground = Brushes.White;
                }
                else if (isFlagged)
                {
                    border.Background = (SolidColorBrush)FindResource("WarningOrange");
                    btn.Foreground = Brushes.White;
                    // Nếu đã trả lời mà có cờ thì thêm gạch chân hoặc gì đó? Tạm thời chỉ màu cam.
                }
                else if (isAnswered)
                {
                    border.Background = (SolidColorBrush)FindResource("SuccessGreen");
                    btn.Foreground = Brushes.White;
                }
                else
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                    btn.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                }
            }
        }

        private void MarkNavButtonAnswered(int index)
        {
            HighlightNavButton(_examService.CurrentIndex);
        }

        // ═══════════════════════════════════════
        //         ĐIỀU HƯỚNG CÂU HỎI
        // ═══════════════════════════════════════
        public void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_examService.CurrentIndex > 0)
                ShowQuestion(_examService.CurrentIndex - 1);
        }

        public void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_examService.CurrentIndex < _examService.ExamQuestions.Count - 1)
                ShowQuestion(_examService.CurrentIndex + 1);
            else
            {
                // Câu cuối → hỏi nộp bài
                var r = MessageBox.Show(
                    "Bạn đã đến câu cuối cùng.\nBạn có muốn nộp bài không?",
                    "Nộp bài", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) ShowResult();
            }
        }

        private void btnFlag_Click(object sender, RoutedEventArgs e)
        {
            int index = _examService.CurrentIndex;
            if (_examService.FlaggedQuestions.Contains(index))
                _examService.FlaggedQuestions.Remove(index);
            else
                _examService.FlaggedQuestions.Add(index);

            UpdateFlagButtonUI();
            HighlightNavButton(_examService.CurrentIndex);
        }

        // ═══════════════════════════════════════
        //         NỘP BÀI
        // ═══════════════════════════════════════
        public void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            int unanswered = _examService.ExamQuestions.Count - _examService.UserAnswers.Count;
            string msg = unanswered > 0
                ? $"Bạn còn {unanswered} câu chưa trả lời.\nBạn có chắc muốn nộp bài không?"
                : "Bạn đã trả lời tất cả các câu.\nXác nhận nộp bài?";

            var r = MessageBox.Show(msg, "Xác nhận nộp bài",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                TN.Services.SoundService.PlayAlert();
                ShowResult();
            }
        }

        // ═══════════════════════════════════════
        //         HIỂN THỊ KẾT QUẢ
        // ═══════════════════════════════════════
        private void ShowResult()
        {
            _timer?.Stop();

            var result = _resultService.CalculateResult(_examService.ExamQuestions, _examService.UserAnswers);

            //Thực hiện lưu lịch sử vào ổ cứng
            try {
                //Tính thời gian đã làm(Nếu là luyện tập thì gán = 0)
                int timeSpent = _examService.CurrentMode == SessionMode.Exam ? (int.Parse(txtTimeLimit.Text) * 60) - _remainingSeconds : 0;
                var newRecord = new HistoryRecord {
                    ExamDate = DateTime.Now,
                    Mode = _examService.CurrentMode.ToString(),
                    TotalQuestions = _examService.ExamQuestions.Count,
                    CorrectAnswers = result.Correct,
                    Score = result.Score,
                    TimeSpentSeconds = timeSpent,
                    WrongQuestions = _examService.ExamQuestions
                        .Where((q, idx) => _examService.UserAnswers.ContainsKey(idx) && _examService.UserAnswers[idx] != q.CorrectIndex)
                        .ToList()
                };
                _historyService.SaveRecord(newRecord);
            }
            catch (Exception ex) {
                MessageBox.Show($"Lỗi khi lưu lịch sử: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Cập nhật số liệu
            txtFinalScore.Text = result.Score.ToString("F1");
            txtFinalCorrect.Text = $"{result.Correct}/{_examService.ExamQuestions.Count}";

            txtResultSub.Text = $"Đúng {result.Correct}/{_examService.ExamQuestions.Count} câu • Còn lại: {txtTimer.Text}";

            if (result.Score >= 8)
            {
                txtResultTitle.Text = "XUẤT SẮC! 🏆";
                txtResultTitle.Foreground = (SolidColorBrush)FindResource("SuccessGreen");
            }
            else if (result.Score >= 5)
            {
                txtResultTitle.Text = "ĐẠT YÊU CẦU ✓";
                txtResultTitle.Foreground = (SolidColorBrush)FindResource("WarningOrange");
            }
            else
            {
                txtResultTitle.Text = "CHƯA ĐẠT ✗";
                txtResultTitle.Foreground = (SolidColorBrush)FindResource("DangerRed");
            }

            // Vẽ danh sách chi tiết
            BuildResultList(result.Correct);

            //Xóa file AutoSave khi bài thi kết thúc
            _examService.ClearAutoSave();
            _timer.Stop();

            // Chuyển màn hình
            pnlExam.Visibility = Visibility.Collapsed;
            pnlResult.Visibility = Visibility.Visible;
        }

        private void BuildResultList(int correct)
        {
            resultList.Items.Clear();

            for (int i = 0; i < _examService.ExamQuestions.Count; i++)
            {
                var q = _examService.ExamQuestions[i];
                bool answered = _examService.UserAnswers.TryGetValue(i, out int userAns);
                bool isRight = answered && userAns == q.CorrectIndex;

                // Container cho mỗi câu
                var row = new Border
                {
                    Background = isRight
                        ? new SolidColorBrush(Color.FromArgb(30, 0, 200, 83))
                        : new SolidColorBrush(Color.FromArgb(30, 255, 23, 68)),
                    BorderBrush = isRight
                        ? new SolidColorBrush(Color.FromArgb(80, 0, 200, 83))
                        : new SolidColorBrush(Color.FromArgb(80, 255, 23, 68)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var sp = new StackPanel();

                // Dòng 1: số câu + trạng thái
                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                header.Children.Add(new TextBlock
                {
                    Text = $"Câu {i + 1}",
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 155, 180)),
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas")
                });

                var statusBadge = new TextBlock
                {
                    Text = isRight ? "✓ ĐÚNG" : (answered ? "✗ SAI" : "— BỎ QUA"),
                    Foreground = isRight
                        ? new SolidColorBrush(Color.FromRgb(0, 200, 83))
                        : (answered
                            ? new SolidColorBrush(Color.FromRgb(255, 23, 68))
                            : new SolidColorBrush(Color.FromRgb(255, 215, 0))),
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold
                };
                Grid.SetColumn(statusBadge, 1);
                header.Children.Add(statusBadge);
                sp.Children.Add(header);

                // Dòng 2: nội dung câu hỏi
                sp.Children.Add(new TextBlock
                {
                    Text = q.Text,
                    Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40)), // xám đậm / gần đen
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 6)
                });

                // Dòng 3: đáp án đúng (và đáp án sai nếu có)
                string correctText = q.Options.Count >= q.CorrectIndex
                    ? q.Options[q.CorrectIndex - 1] : "?";
                sp.Children.Add(new TextBlock
                {
                    Text = $"✓ Đáp án đúng: {correctText}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 83)),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });

                if (answered && !isRight)
                {
                    string wrongText = q.Options.Count >= userAns ? q.Options[userAns - 1] : "?";
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"✗ Bạn chọn: {wrongText}",
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 107)),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }

                row.Child = sp;
                resultList.Items.Add(row);
            }
        }

        // ═══════════════════════════════════════
        //         THI LẠI
        // ═══════════════════════════════════════
        public void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            pnlResult.Visibility = Visibility.Collapsed;
            pnlExam.Visibility = Visibility.Collapsed;
            pnlSetup.Visibility = Visibility.Visible;
            pnlExamInfo.Visibility = Visibility.Collapsed;
            pnlTimer.Visibility = Visibility.Collapsed;

            txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        }

        public void btnBack_Click(object sender, RoutedEventArgs e) => TryClose(goHome: true);

        public void btnPracticeWrong_Click(object sender, RoutedEventArgs e)
        {
            if (!_licenseService.IsPremiumUser())
            {
                MessageBox.Show("Đây là tính năng PREMIUM.\nHãy nâng cấp bản quyền để sử dụng luyện tập thông minh!", 
                                "Nâng cấp Premium", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var wrongQuestions = _historyService.GetAllUniqueWrongQuestions();
            if (wrongQuestions == null || wrongQuestions.Count == 0)
            {
                MessageBox.Show("Bạn chưa có câu hỏi sai nào trong lịch sử để ôn tập!", 
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Thiết lập bài thi với danh sách câu sai
            _examService.ExamQuestions = wrongQuestions;
            _examService.CurrentMode = SessionMode.Practice; // Luôn là chế độ luyện tập
            _examService.CurrentIndex = 0;
            _examService.UserAnswers.Clear();
            _remainingSeconds = 0; // Hoặc gán theo thời gian mặc định luyện tập

            pnlSetup.Visibility = Visibility.Collapsed;
            pnlExam.Visibility = Visibility.Visible;
            pnlExamInfo.Visibility = Visibility.Visible;
            pnlTimer.Visibility = Visibility.Collapsed;

            UpdateProgressBar();
            BuildNavButtons();
            ShowQuestion(0);
        }

        // ═══════════════════════════════════════
        //         ĐÓNG CỬA SỔ
        // ═══════════════════════════════════════
        private void BtnClose_Click(object sender, RoutedEventArgs e) => TryClose();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) TryClose();
        }

        private void TryClose(bool goHome = false)
        {
            if (_timer != null && _timer.IsEnabled)
            {
                var r = MessageBox.Show(
                    "Bài thi đang diễn ra.\nBạn có chắc muốn thoát không?",
                    "Xác nhận thoát", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                _timer.Stop();
            }

            if (goHome)
            {
                var home = new HomeWindow();
                home.Show();
            }
            Close();
        }
    



    }
}