using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TN.Models;

namespace TN.Services
{
    public class ResultService
    {
        public ExamResult CalculateResult(List<Question> questions, Dictionary<int, int> userAnswers){
            int correct  = 0;
            int wrong = 0;
            int skip = 0;

            for(int i = 0; i < questions.Count; i++){
                if(!userAnswers.TryGetValue(i, out int userAns)){
                    skip++;
                    continue;
                }
                if(userAns == questions[i].CorrectIndex)
                    correct++;
                else
                    wrong++;
            }
            double score = Math.Round((double)correct / questions.Count * 10, 2);
            return new ExamResult{
                Correct = correct,
                Wrong = wrong,
                Skip = skip,
                Score = score
            };
        }
    }
}
