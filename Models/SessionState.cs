using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TN.Models
{
    public class SessionState
    {
        public List<Question> ExamQuestions { get; set; } = new();
        public Dictionary<int, int> UserAnswers { get; set; } = new();
        public int CurrentIndex { get; set; }
        
        // Cố tình dùng string cho dễ lưu JSON thay vì Enum
        public string CurrentMode { get; set; } 
        public int RemainingSeconds { get; set; }
    }
}
