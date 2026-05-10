using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TN.Models
{
    public class Collection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string FolderPath { get; set; }
        
        // Chứa tất cả các câu hỏi thuộc bộ đề này
        public List<Question> Questions { get; set; } = new();
        public Collection(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Untited Collection" : name;
        }
    }
}
