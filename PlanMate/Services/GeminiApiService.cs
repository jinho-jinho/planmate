using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlanMate.Models;

namespace PlanMate.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20:generateContent";
        private const string ApiKey = "AIzaSyBSp8l09lNLK_KUJQsw6NOPTIeicMB1t08"; //무료 키임 사용 ㄴㄴ 

        public GeminiApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetResponseAsync(string userPrompt)
        {
            var requestData = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = userPrompt } } }
                }
            };

            var requestJson = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{ApiUrl}?key={ApiKey}", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"Error {response.StatusCode}: {responseJson}";

                dynamic result = JsonConvert.DeserializeObject(responseJson);
                return result?.candidates?[0]?.content?.parts?[0]?.text ?? "No response";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<string> GetSmartSummaryAsync(
            List<TaskItem> taskList,
            List<MemoItem> memoList,
            List<ScheduleItem> scheduleList,
            string userInstruction)
        {
            var sb = new StringBuilder();
            sb.AppendLine("당신은 사용자 일정/시간표/메모를 도와주는 AI 비서입니다.");
            sb.AppendLine("요청에 따라 아래 JSON 데이터를 참고하여 요약 또는 조언을 생성하세요.");
            sb.AppendLine();
            sb.AppendLine($"사용자 요청: {userInstruction}");
            sb.AppendLine();

            if (userInstruction.Contains("일정"))
            {
                var minimalTasks = taskList.Select(task => new
                {
                    title = task.Name,
                    startDate = task.StartDate.ToString("yyyy-MM-dd"),
                    startTime = task.StartTime,
                    endDate = task.EndDate.ToString("yyyy-MM-dd"),
                    endTime = task.EndTime,
                    importance = task.Importance,
                    details = task.Details,
                    isCompleted = task.IsCompleted,
                    relatedDocs = task.RelatedDocs
                }).ToList();

                sb.AppendLine("일정 목록 (tasks):");
                sb.AppendLine(JsonConvert.SerializeObject(minimalTasks, Formatting.Indented));
                sb.AppendLine();
            }

            if (userInstruction.Contains("시간표"))
            {
                var minimalSchedules = scheduleList.Select(s => new
                {
                    day = s.Day.ToString(), // e.g., "Monday"
                    startTime = s.StartTime.ToString(@"hh\:mm"),
                    endTime = s.EndTime.ToString(@"hh\:mm"),
                    title = s.Title
                }).ToList();

                sb.AppendLine("시간표 목록 (schedules):");
                sb.AppendLine(JsonConvert.SerializeObject(minimalSchedules, Formatting.Indented));
                sb.AppendLine();
            }

            if (userInstruction.Contains("메모"))
            {
                var minimalMemos = memoList.Select(m => new
                {
                    title = m.Title,
                    content = m.Content,
                    createdAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList();

                sb.AppendLine("메모 목록 (memos):");
                sb.AppendLine(JsonConvert.SerializeObject(minimalMemos, Formatting.Indented));
                sb.AppendLine();
            }

            return await GetResponseAsync(sb.ToString());
        }
    }
}
