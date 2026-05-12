using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TN.Models
{
    public class Question
    {
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; }  // 1-based
        public string? ImagePath { get; set; }
    }
}
