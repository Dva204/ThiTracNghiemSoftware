using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.Json;
using TN.Models;

namespace TN.Services
{
    public class HistoryService
    {
        private readonly string _folderPath;
        private readonly string _filePath;

        public HistoryService(){
            //Đường dẫn cố định
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _folderPath = Path.Combine(baseDir, "Storage", "History");
            _filePath = Path.Combine(_folderPath, "history.json");
        }
        //Lấy toàn bộ lịch sử từ file Json
        public List<HistoryRecord> LoadHistory(){
            if(!File.Exists(_filePath)){
                return new List<HistoryRecord>();
            }
            try{
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<HistoryRecord>>(json) ?? new List<HistoryRecord>();
            }
            catch{
                return new List<HistoryRecord>();
            }
        }
        //Thêm mới 1 kỉ lục và ghi đề lên ổ cứng
        public void SaveRecord(HistoryRecord newRecord){
            if(!Directory.Exists(_folderPath)){
                Directory.CreateDirectory(_folderPath);
            }

            //1. Load lịch sử cũ
            var history = LoadHistory();

            //2. Thêm thành tích mới
            history.Add(newRecord);

            //3. Format thành Json
            var options = new JsonSerializerOptions{ WriteIndented = true};
            string json = JsonSerializer.Serialize(history, options);

            //4. Ghi đè file
            File.WriteAllText(_filePath, json);
        }

        public void ClearHistory()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        // Xóa một lịch sử chỉ định dưa trên mã ID
        public void DeleteRecord(string id)
        {
            var history = LoadHistory();
            
            // Tìm kiếm đối tượng có Id trùng khớp
            var itemToDelete = history.FirstOrDefault(x => x.Id == id);
            
            if (itemToDelete != null)
            {
                history.Remove(itemToDelete);
                // Ghi lại danh sách cẩn thận xuống file
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(history, options);
                File.WriteAllText(_filePath, json);
            }
        }

        public List<Question> GetAllUniqueWrongQuestions()
        {
            var history = LoadHistory();
            var allWrongs = new List<Question>();

            foreach (var record in history)
            {
                if (record.WrongQuestions != null)
                {
                    allWrongs.AddRange(record.WrongQuestions);
                }
            }

            // Lọc các bản ghi trùng lặp dựa trên nội dung câu hỏi
            return allWrongs
                .GroupBy(q => q.Text)
                .Select(g => g.First())
                .ToList();
        }
    }
}
