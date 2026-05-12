using System;
using System.Collections.Generic;

namespace TN.Models
{
    public class ExamBank
    {
        public string Title { get; set; } = "Bộ đề tự soạn";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public List<Question> Questions { get; set; } = new List<Question>();
    }
}
