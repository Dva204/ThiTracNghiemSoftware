using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace TN.Services
{
    public class StorageService
    {
        private readonly string _baseDbPath;

        public StorageService(){
            //Tự động tìm đường dẫn chưa file chạy của phần mềm và trỏ đến thư mục Storage
            _baseDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "QuestionBanks");

            //Nếu chưa có thư mục thì tự động tạo
            if(!Directory.Exists(_baseDbPath)){
                Directory.CreateDirectory(_baseDbPath);
            }
        }

        public string ImportFile(string sourceFilePath, string targetFileName){
            if(!File.Exists(sourceFilePath)){
                throw new FileNotFoundException("Không tìm thấy file nguồn. ");
            }
            string destPath = Path.Combine(_baseDbPath, targetFileName);

            //Lệnh copy file của hệ điều hành, ovewrite = true để ghi đè nếu trùng tên gốc
            File.Copy(sourceFilePath, destPath, overwrite: true);

            return destPath; // trả về đường dẫn tuyệt đối của file sau khi copy
        }

        public List<string> GetCategories() {
            if (!Directory.Exists(_baseDbPath)) return new List<string>();
            return Directory.GetDirectories(_baseDbPath)
                            .Select(Path.GetFileName)
                            .ToList();
        }

        public List<string> GetBanksByCategory(string category) {
            string categoryPath = Path.Combine(_baseDbPath, category);
            if (!Directory.Exists(categoryPath)) return new List<string>();
            
            return Directory.GetFiles(categoryPath, "*.docx")
                            .Select(Path.GetFileName)
                            .ToList();
        }

        public string ImportFileToCategory(string sourceFilePath, string targetFileName, string category) {
            string categoryPath = Path.Combine(_baseDbPath, category);
            if (!Directory.Exists(categoryPath)) Directory.CreateDirectory(categoryPath);

            string destPath = Path.Combine(categoryPath, targetFileName);
            File.Copy(sourceFilePath, destPath, overwrite: true);
            return destPath;
        }

        private readonly string _activeMarkerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "active_bank.txt");
        private readonly string _activeCategoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "active_category.txt");

        public void SetActiveBank(string category, string fileName){
            File.WriteAllText(_activeCategoryPath, category);
            File.WriteAllText(_activeMarkerPath, fileName);
        }

        public string GetActiveBankPath(){
            if(File.Exists(_activeMarkerPath) && File.Exists(_activeCategoryPath)){
                string category = File.ReadAllText(_activeCategoryPath);
                string fileName = File.ReadAllText(_activeMarkerPath);
                return Path.Combine(_baseDbPath, category, fileName);
            }
            return null;
        }

        public string GetActiveCategory() => File.Exists(_activeCategoryPath) ? File.ReadAllText(_activeCategoryPath) : null;
        public string GetActiveFileName() => File.Exists(_activeMarkerPath) ? File.ReadAllText(_activeMarkerPath) : null;

        public void RenameCategory(string oldName, string newName){
            string oldPath = Path.Combine(_baseDbPath, oldName);
            string newPath = Path.Combine(_baseDbPath, newName);
            Directory.Move(oldPath, newPath);

            if(GetActiveCategory() == oldName){
                SetActiveBank(newName, GetActiveFileName());
            }
        }

        public void DeleteCategory(string category){
            string categoryPath = Path.Combine(_baseDbPath, category);
            if(Directory.Exists(categoryPath)){
                Directory.Delete(categoryPath, recursive: true);
            }
        }
    }
}
