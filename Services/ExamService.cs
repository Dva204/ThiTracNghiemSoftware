using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TN.Models;
using System.IO;
using System.Text.Json;

namespace TN.Services
{
    //Tạo 1 danh sách phân loại chế độ
    public enum SessionMode
    {
        Exam,
        Practice
    }
    public class ExamService
    {
        //Danh sách các câu hỏi trong đề thi (Random)
        public List<Question> ExamQuestions{get; set;} = new();
        //Đáp án của người dùng: index → 1/2/3/4
        public Dictionary<int, int> UserAnswers{get; set;} = new();
        //Danh sách câu hỏi đánh dấu để xem lại
        public HashSet<int> FlaggedQuestions { get; set; } = new();
        //Vị trí câu hỏi hiện tại
        public int CurrentIndex { get; set; } = 0;

        //Cờ lưu chế độ hiện tại
        public SessionMode CurrentMode{get; set;}

        public bool IsRealExam => CurrentMode == SessionMode.Exam;

        // Mặc định luôn là Exam nếu không ai truyền tham số
        public void StartExam(List<Question> bank, int targetCount, SessionMode mode = SessionMode.Exam){
            var rng = new Random();
            ExamQuestions = bank.OrderBy(_ => rng.Next()).Take(targetCount).ToList();
            UserAnswers.Clear();
            FlaggedQuestions.Clear();
            CurrentIndex = 0;
            CurrentMode = mode;
        }
        //Lưu câu trả lời người dùng
        public void SaveUserAnswer(int questionIndex, int answerValue){
            if(questionIndex >= 0 && questionIndex < ExamQuestions.Count){
                UserAnswers[questionIndex] = answerValue;
            }
        }

        // Lưu lại trạng thái
        public void AutoSaveSession(int remainingSeconds){
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Autosave");
            if(!Directory.Exists(folder)){
                Directory.CreateDirectory(folder);
            }
            string file = Path.Combine(folder, "session.json");
            var state = new SessionState
            {
                ExamQuestions = this.ExamQuestions,
                UserAnswers = this.UserAnswers,
                FlaggedQuestions = this.FlaggedQuestions,
                CurrentIndex = this.CurrentIndex,
                CurrentMode = this.CurrentMode.ToString(),
                RemainingSeconds = remainingSeconds
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(state, options);
            File.WriteAllText(file, json);
        }

        public void ClearAutoSave(){
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Autosave", "session.json");
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
