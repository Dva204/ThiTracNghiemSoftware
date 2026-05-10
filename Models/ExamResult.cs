using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TN.Models
{
    public class ExamResult
    {
        public int Correct{get; set;}
        public int Wrong{get; set;}
        public int Skip{get; set;}
        public double Score{get; set;}
    }
}
