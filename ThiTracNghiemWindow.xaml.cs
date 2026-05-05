using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

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
        private List<Question> _examQuestions = new();
        private Dictionary<int, int> _userAnswers = new(); // index → 1/2/3/4
        private int _currentIndex = 0;

        // ═══════════════════════════════════════
        //              ĐỒNG HỒ
        // ═══════════════════════════════════════
        private DispatcherTimer _timer;
        private int _remainingSeconds;

        // ═══════════════════════════════════════
        //              KHỞI TẠO
        // ═══════════════════════════════════════
        public ThiTracNghiemWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Load câu hỏi từ file Word
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "tracnghiem.docx");

            _allQuestions = LoadQuestionsFromDocx(path);

            // Cập nhật UI thống kê kho câu
            UpdateCountLabels();
        }

        // ═══════════════════════════════════════
        //         ĐỌC FILE WORD (OpenXML)
        // ═══════════════════════════════════════

        // Regex matches option prefixes: a. b. c. d. (Latin + common Cyrillic lookalikes)
        private static readonly System.Text.RegularExpressions.Regex _optionPrefix =
            new System.Text.RegularExpressions.Regex(
                @"^[a-dA-Dабвгдс][.]\s",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        private static bool IsAnswerIndex(string s) =>
            s.Trim() == "1" || s.Trim() == "2" ||
            s.Trim() == "3" || s.Trim() == "4";

        private static bool IsOptionLine(string s) =>
            _optionPrefix.IsMatch(s);

        private List<Question> LoadQuestionsFromDocx(string filePath)
        {
            var list = new List<Question>();

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    $"Không tìm thấy file:\n{filePath}\n\nHãy đặt file tracnghiem.docx vào thư mục Resources.",
                    "Thiếu file", MessageBoxButton.OK, MessageBoxImage.Warning);
                return list;
            }

            using var doc = DocumentFormat.OpenXml.Packaging
                                .WordprocessingDocument.Open(filePath, false);

            // Collect all non-empty, #-terminated lines (strip the trailing #)
            var contentLines = doc.MainDocumentPart!.Document.Body!
                                  .Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                                  .Select(p => p.InnerText.Trim())
                                  .Where(t => t.Length > 0 && t.EndsWith("#") && t != "#")
                                  .Select(t => t.TrimEnd('#').Trim())
                                  .Where(t => t.Length > 0)
                                  .ToList();

            int i = 0;
            while (i < contentLines.Count)
            {
                string line = contentLines[i];

                // Skip stray answer-index lines (out-of-order)
                if (IsAnswerIndex(line)) { i++; continue; }

                // Skip old difficulty markers
                string upper = line.ToUpperInvariant();
                if (upper == "$DỄ" || upper == "$DE" || upper == "$TRUNG BÌNH" ||
                    upper == "$TRUNG BINH" || upper == "$KHÓ" || upper == "$KHO")
                { i++; continue; }

                // Skip dangling option lines (no question before them)
                if (IsOptionLine(line)) { i++; continue; }

                // This should be a question text
                string qText = System.Text.RegularExpressions.Regex
                                     .Replace(line, @"^\d+\.\s*", "").Trim();
                i++;

                // Collect exactly 4 option lines
                var opts = new List<string>();
                while (i < contentLines.Count && opts.Count < 4 && IsOptionLine(contentLines[i]))
                {
                    opts.Add(contentLines[i]);
                    i++;
                }

                if (opts.Count != 4)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SKIP] Only {opts.Count} options for: '{qText[..Math.Min(40, qText.Length)]}'");
                    continue;
                }

                // Expect answer index (1-4)
                if (i < contentLines.Count && IsAnswerIndex(contentLines[i]))
                {
                    int idx = int.Parse(contentLines[i].Trim());
                    i++;

                    list.Add(new Question
                    {
                        Text = qText,
                        // Strip leading "a. " / "b. " / "c. " / "d. " prefix from options
                        Options = opts.Select(o =>
                            System.Text.RegularExpressions.Regex.Replace(o, @"^[^\s]+\s+", "").Trim()
                        ).ToList(),
                        CorrectIndex = idx
                    });
                }
                else
                {
                    string got = i < contentLines.Count
                        ? contentLines[i][..Math.Min(40, contentLines[i].Length)] : "EOF";
                    System.Diagnostics.Debug.WriteLine(
                        $"[SKIP] No valid answer index for: '{qText[..Math.Min(40, qText.Length)]}' — got: '{got}'");
                }
            }

            return list;
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
        private void btnStartExam_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSetup()) return;

            int total = int.Parse(txtTotalQuestions.Text);
            var rng = new Random();

            _examQuestions = _allQuestions.OrderBy(_ => rng.Next()).Take(total).ToList();
            _userAnswers = new Dictionary<int, int>();
            _currentIndex = 0;

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

            ShowQuestion(_currentIndex);
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
            if (index < 0 || index >= _examQuestions.Count) return;

            _currentIndex = index;
            var q = _examQuestions[index];

            // (Đã bỏ badge độ khó)

            txtQuestionNumber.Text = $"Câu {index + 1} / {_examQuestions.Count}";
            txtQuestion.Text = q.Text;

            // Gán đáp án vào Tag để RadioButton dùng
            rbA.Tag = q.Options.Count > 0 ? q.Options[0] : "";
            rbB.Tag = q.Options.Count > 1 ? q.Options[1] : "";
            rbC.Tag = q.Options.Count > 2 ? q.Options[2] : "";
            rbD.Tag = q.Options.Count > 3 ? q.Options[3] : "";

            // Reset chọn trước khi set
            rbA.IsChecked = false;
            rbB.IsChecked = false;
            rbC.IsChecked = false;
            rbD.IsChecked = false;

            // Khôi phục lựa chọn đã lưu
            if (_userAnswers.TryGetValue(index, out int saved))
            {
                var rbs = new[] { rbA, rbB, rbC, rbD };
                rbs[saved - 1].IsChecked = true;
            }

            // Cập nhật header
            txtProgress.Text = $"{index + 1}/{_examQuestions.Count}";
            txtAnswered.Text = _userAnswers.Count.ToString();

            // Cập nhật progress bar
            UpdateProgressBar();

            // Highlight nút điều hướng đang xem
            HighlightNavButton(index);

            // Nút prev
            btnPrev.IsEnabled = index > 0;
            btnPrev.Opacity = index > 0 ? 1.0 : 0.4;
        }

        // ═══════════════════════════════════════
        //         LƯU ĐÁP ÁN
        // ═══════════════════════════════════════
        private void Answer_Checked(object sender, RoutedEventArgs e)
        {
            int ans = 0;
            if (sender == rbA) ans = 1;
            else if (sender == rbB) ans = 2;
            else if (sender == rbC) ans = 3;
            else if (sender == rbD) ans = 4;

            if (ans > 0)
            {
                _userAnswers[_currentIndex] = ans;
                MarkNavButtonAnswered(_currentIndex);
                txtAnswered.Text = _userAnswers.Count.ToString();
                UpdateProgressBar();
            }
        }

        // ═══════════════════════════════════════
        //         THANH TIẾN ĐỘ
        // ═══════════════════════════════════════
        private void UpdateProgressBar()
        {
            int total = _examQuestions.Count;
            int answered = _userAnswers.Count;

            txtProgressBar.Text = $"{answered} / {total} câu đã trả lời";

            // Tính chiều rộng fill (cần đợi layout xong)
            Dispatcher.InvokeAsync(() =>
            {
                double parentWidth = progressFill.ActualWidth == 0
                    ? ((Border)progressFill.Parent).ActualWidth
                    : ((Border)progressFill.Parent).ActualWidth;

                double ratio = total > 0 ? (double)answered / total : 0;
                progressFill.Width = parentWidth * ratio;
            }, DispatcherPriority.Loaded);
        }

        // ═══════════════════════════════════════
        //         BẢNG ĐIỀU HƯỚNG
        // ═══════════════════════════════════════
        private void BuildNavButtons()
        {
            wrapNavButtons.Children.Clear();
            for (int i = 0; i < _examQuestions.Count; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Style = FindResource("NavBtnStyle") as Style,
                    Tag = i
                };
                btn.Click += (s, e) => ShowQuestion(idx);
                wrapNavButtons.Children.Add(btn);
            }
        }

        private void HighlightNavButton(int index)
        {
            for (int i = 0; i < wrapNavButtons.Children.Count; i++)
            {
                if (wrapNavButtons.Children[i] is not Button btn) continue;

                bool isCurrent = i == index;
                bool isAnswered = _userAnswers.ContainsKey(i);

                if (isCurrent)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(13, 33, 55));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 212, 255));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(0, 212, 255));
                }
                else if (isAnswered)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(13, 42, 26));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 136));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136));
                }
                else
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 45, 69));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(139, 155, 180));
                }
            }
        }

        private void MarkNavButtonAnswered(int index)
        {
            HighlightNavButton(_currentIndex);
        }

        // ═══════════════════════════════════════
        //         ĐIỀU HƯỚNG CÂU HỎI
        // ═══════════════════════════════════════
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
                ShowQuestion(_currentIndex - 1);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _examQuestions.Count - 1)
                ShowQuestion(_currentIndex + 1);
            else
            {
                // Câu cuối → hỏi nộp bài
                var r = MessageBox.Show(
                    "Bạn đã đến câu cuối cùng.\nBạn có muốn nộp bài không?",
                    "Nộp bài", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) ShowResult();
            }
        }

        // ═══════════════════════════════════════
        //         NỘP BÀI
        // ═══════════════════════════════════════
        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            int unanswered = _examQuestions.Count - _userAnswers.Count;
            string msg = unanswered > 0
                ? $"Bạn còn {unanswered} câu chưa trả lời.\nBạn có chắc muốn nộp bài không?"
                : "Bạn đã trả lời tất cả các câu.\nXác nhận nộp bài?";

            var r = MessageBox.Show(msg, "Xác nhận nộp bài",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) ShowResult();
        }

        // ═══════════════════════════════════════
        //         HIỂN THỊ KẾT QUẢ
        // ═══════════════════════════════════════
        private void ShowResult()
        {
            _timer?.Stop();

            int correct = 0, wrong = 0, skip = 0;

            for (int i = 0; i < _examQuestions.Count; i++)
            {
                if (!_userAnswers.TryGetValue(i, out int userAns))
                {
                    skip++;
                    continue;
                }
                if (userAns == _examQuestions[i].CorrectIndex) correct++;
                else wrong++;
            }

            double score = Math.Round((double)correct / _examQuestions.Count * 10, 1);

            // Cập nhật số liệu
            txtScore.Text = score.ToString("F1");
            txtCorrectCount.Text = correct.ToString();
            txtWrongCount.Text = wrong.ToString();
            txtSkipCount.Text = skip.ToString();

            txtResultSub.Text = $"Đúng {correct}/{_examQuestions.Count} câu • Thời gian còn lại: {txtTimer.Text}";

            if (score >= 8)
            {
                txtResultTitle.Text = "XUẤT SẮC! 🏆";
                txtResultTitle.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136));
            }
            else if (score >= 5)
            {
                txtResultTitle.Text = "ĐẠT YÊU CẦU ✓";
                txtResultTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            }
            else
            {
                txtResultTitle.Text = "CHƯA ĐẠT ✗";
                txtResultTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 23, 68));
            }

            // Vẽ danh sách chi tiết
            BuildResultList(correct);

            // Chuyển màn hình
            pnlExam.Visibility = Visibility.Collapsed;
            pnlResult.Visibility = Visibility.Visible;
        }

        private void BuildResultList(int correct)
        {
            resultList.Items.Clear();

            for (int i = 0; i < _examQuestions.Count; i++)
            {
                var q = _examQuestions[i];
                bool answered = _userAnswers.TryGetValue(i, out int userAns);
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
        private void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            pnlResult.Visibility = Visibility.Collapsed;
            pnlExam.Visibility = Visibility.Collapsed;
            pnlSetup.Visibility = Visibility.Visible;
            pnlExamInfo.Visibility = Visibility.Collapsed;
            pnlTimer.Visibility = Visibility.Collapsed;

            txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        }

        // ═══════════════════════════════════════
        //         ĐÓNG CỬA SỔ
        // ═══════════════════════════════════════
        private void BtnClose_Click(object sender, RoutedEventArgs e) => TryClose();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) TryClose();
        }

        private void TryClose()
        {
            if (_timer != null && _timer.IsEnabled)
            {
                var r = MessageBox.Show(
                    "Bài thi đang diễn ra.\nBạn có chắc muốn thoát không?",
                    "Xác nhận thoát", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                _timer.Stop();
            }
            Close();
        }
    }

    // ═══════════════════════════════════════
    //         MODEL CÂU HỎI
    // ═══════════════════════════════════════
    public class Question
    {
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; }  // 1-based
    }
}