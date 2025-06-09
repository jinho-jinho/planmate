using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PlanMate.Models;

namespace PlanMate.Services
{
    public class JsonStorageService
    {
        private readonly string _filePath;
        public JsonStorageService(string fileName)
            => _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        public List<MemoItem> LoadMemos()
        {
            if (!File.Exists(_filePath)) return new List<MemoItem>();
            try { return JsonSerializer.Deserialize<List<MemoItem>>(File.ReadAllText(_filePath)) ?? new List<MemoItem>(); }
            catch { return new List<MemoItem>(); }
        }

        public void SaveMemos(IEnumerable<MemoItem> memos)
            => File.WriteAllText(_filePath, JsonSerializer.Serialize(memos, new JsonSerializerOptions { WriteIndented = true }));
    }
}