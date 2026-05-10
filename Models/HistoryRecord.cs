using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TN.Models
{
    public class HistoryRecord
    {
        public string Id {get; set; } = Guid.NewGuid().ToString();
        public DateTime ExamDate {get; set; } 
        public string Mode {get; set; } // exam hoặc practice
        public int TotalQuestions {get; set; }
        public int CorrectAnswers {get; set; }
        public double Score {get; set; }
        public int TimeSpentSeconds {get; set; }
        public List<Question> WrongQuestions { get; set; } = new List<Question>();
    }
}
