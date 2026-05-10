using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TN.Models;
using TN.Services;

namespace TN.Views
{
    public partial class StatisticsWindow : Window
    {
        private readonly HistoryService _historyService = new();

        public StatisticsWindow()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            var history = _historyService.LoadHistory();
            if (history == null || history.Count == 0) return;

            // 1. Tổng quan
            txtTotalExams.Text = history.Count.ToString();
            txtAvgScore.Text = $"{history.Average(h => h.Score * 10):F0}%";
            txtStreak.Text = CalculateStreak(history) + " Days";

            // 2. Biểu đồ (7 ngày gần nhất)
            var last7Days = history
                .Where(h => h.ExamDate >= DateTime.Now.AddDays(-7))
                .GroupBy(h => h.ExamDate.Date)
                .Select(g => new ChartItem
                {
                    Label = g.Key.ToString("dd/MM"),
                    Value = g.Average(h => h.Score),
                    ValueText = g.Average(h => h.Score).ToString("F1")
                })
                .OrderBy(c => c.Label)
                .ToList();

            icChart.ItemsSource = last7Days;

            // 3. Chủ đề còn yếu (Dựa trên số lượng câu sai)
            var weakTopics = history
                .SelectMany(h => h.WrongQuestions ?? new List<Question>())
                .GroupBy(q => "General") // Có thể scale theo Category nếu Question có lưu Category
                .Select(g => new { TopicName = g.Key, ErrorCount = g.Count() })
                .OrderByDescending(x => x.ErrorCount)
                .Take(5)
                .ToList();

            icWeakTopics.ItemsSource = weakTopics;
        }

        private int CalculateStreak(List<HistoryRecord> history)
        {
            if (history.Count == 0) return 0;

            var dates = history.Select(h => h.ExamDate.Date).Distinct().OrderByDescending(d => d).ToList();
            int streak = 0;
            DateTime today = DateTime.Today;

            if (dates[0] != today && dates[0] != today.AddDays(-1)) return 0;

            DateTime current = dates[0];
            foreach (var date in dates)
            {
                if (date == current)
                {
                    streak++;
                    current = current.AddDays(-1);
                }
                else break;
            }
            return streak;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var home = new HomeWindow();
            home.Show();
            this.Close();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng Xuất PDF đang được nén và sẽ sớm ra mắt trong bản cập nhật tới!", "Premium Feature");
        }
    }

    public class ChartItem
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public string ValueText { get; set; }
        public double BarHeight => Value * 18; // Scale điểm 10 -> 180px
        public Brush BarBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Value >= 8 ? "#22C55E" : (Value >= 5 ? "#3B82F6" : "#EF4444")));
    }
}
