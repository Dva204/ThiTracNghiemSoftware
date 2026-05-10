using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TN.Models;

namespace TN.Services
{
    public class QuestionParserService
    {
        private static readonly Regex _optionPrefix = new Regex(
            @"^[a-dA-Dабвгдс][.]\s",
            RegexOptions.Compiled);

        public List<Question> LoadQuestionsFromDocx(string filePath)
        {
            var list = new List<Question>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file:\n{filePath}\n\nHãy đặt file docx vào thư mục hợp lệ.");
            }

            using var doc = WordprocessingDocument.Open(filePath, false);

            var contentLines = doc.MainDocumentPart!.Document.Body!
                                  .Elements<Paragraph>()
                                  .Select(p => p.InnerText.Trim())
                                  .Where(t => t.Length > 0 && t.EndsWith("#") && t != "#")
                                  .Select(t => t.TrimEnd('#').Trim())
                                  .Where(t => t.Length > 0)
                                  .ToList();

            int i = 0;
            while (i < contentLines.Count)
            {
                string line = contentLines[i];

                if (IsAnswerIndex(line)) { i++; continue; }

                string upper = line.ToUpperInvariant();
                if (upper == "$DỄ" || upper == "$DE" || upper == "$TRUNG BÌNH" ||
                    upper == "$TRUNG BINH" || upper == "$KHÓ" || upper == "$KHO")
                { i++; continue; }

                if (IsOptionLine(line)) { i++; continue; }

                string qText = Regex.Replace(line, @"^\d+\.\s*", "").Trim();
                i++;

                var opts = new List<string>();
                while (i < contentLines.Count && opts.Count < 4 && IsOptionLine(contentLines[i]))
                {
                    opts.Add(contentLines[i]);
                    i++;
                }

                if (opts.Count != 4) continue;

                if (i < contentLines.Count && IsAnswerIndex(contentLines[i]))
                {
                    int idx = int.Parse(contentLines[i].Trim());
                    i++;

                    list.Add(new Question
                    {
                        Text = qText,
                        Options = opts.Select(o => Regex.Replace(o, @"^[^\s]+\s+", "").Trim()).ToList(),
                        CorrectIndex = idx
                    });
                }
            }

            return list;
        }

        private static bool IsAnswerIndex(string s) =>
            s.Trim() == "1" || s.Trim() == "2" || s.Trim() == "3" || s.Trim() == "4";

        private static bool IsOptionLine(string s) => _optionPrefix.IsMatch(s);
    }
}
