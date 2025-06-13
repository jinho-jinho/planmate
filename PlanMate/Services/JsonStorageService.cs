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
        {
            // Environment.SpecialFolder.LocalApplicationData 경로를 사용하여 파일 경로를 구성합니다.
            // "PlanMate"는 애플리케이션의 고유 폴더 이름입니다.
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate");

            // 디렉토리가 존재하지 않으면 생성합니다.
            Directory.CreateDirectory(appDataFolder);

            _filePath = Path.Combine(appDataFolder, fileName);
        }

        public List<MemoItem> LoadMemos()
        {
            if (!File.Exists(_filePath)) return new List<MemoItem>();
            try
            {
                // 파일에서 JSON을 읽어 MemoItem 리스트로 역직렬화합니다.
                return JsonSerializer.Deserialize<List<MemoItem>>(File.ReadAllText(_filePath)) ?? new List<MemoItem>();
            }
            catch (Exception ex)
            {
                // 오류 발생 시 빈 리스트를 반환하거나 로깅을 할 수 있습니다.
                Console.WriteLine($"Error loading memos: {ex.Message}");
                return new List<MemoItem>();
            }
        }

        public void SaveMemos(IEnumerable<MemoItem> memos)
        {
            // MemoItem 리스트를 JSON으로 직렬화하여 파일에 저장합니다.
            // WriteIndented = true 옵션으로 JSON을 예쁘게 정렬하여 저장합니다.
            File.WriteAllText(_filePath, JsonSerializer.Serialize(memos, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}