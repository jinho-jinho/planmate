using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using PlanMate.Models;

namespace PlanMate.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-04-17:generateContent";
        private const string ApiKey = "AIzaSyBSp8l09lNLK_KUJQsw6NOPTIeicMB1t08"; // 보안 시 별도 관리 권장

        public GeminiApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetResponseAsync(string userPrompt)
        {
            var requestData = new
            {
                contents = new[]
                {
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
                {
                    return $"Error {response.StatusCode}: {responseJson}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseJson);
                return result?.candidates?[0]?.content?.parts?[0]?.text ?? "No response";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<string> GetScheduleSummaryAsync(List<TaskItem> scheduleList, string userInstruction)
        {
            var minimalSchedule = scheduleList.Select(task => new
            {
                title = task.Name,
                startDate = task.StartDate.ToString("yyyy-MM-dd"),
                startTime = task.StartTime,
                endDate = task.EndDate.ToString("yyyy-MM-dd"),
                endTime = task.EndTime,
                importance = task.Importance,
                details = task.Details,
                isCompleted = task.IsCompleted
            }).ToList();

            string jsonSchedule = JsonConvert.SerializeObject(minimalSchedule, Newtonsoft.Json.Formatting.Indented);

            string prompt = $@"
다음은 사용자의 일정 목록입니다. 각 일정에는 제목, 시작일/시간, 종료일/시간, 중요도, 설명, 완료 여부가 포함되어 있습니다.

당신의 임무는 다음 사용자 요청을 바탕으로 응답을 생성하는 것입니다:
- 사용자 요청: {userInstruction}

일정 목록 (JSON):
{jsonSchedule}
";

            return await GetResponseAsync(prompt);
        }
    }
}
