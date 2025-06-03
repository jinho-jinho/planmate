using PlanMate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PlanMate.Services; // GeminiApiService 위치 맞게 수정

namespace PlanMate.Views
{
    public partial class AiChatWindow : Window
    {
        private readonly GeminiApiService _geminiService;
        private readonly List<TaskItem> taskList;

        public AiChatWindow(List<TaskItem> taskItems) // 외부에서 일정 주입받음
        {
            InitializeComponent();
            _geminiService = new GeminiApiService();
            taskList = taskItems; // 전달받은 실제 일정
        }

        private async void SendMessageAsync(string message)
        {
            ChatList.Items.Add("👤 " + message);
            ChatList.Items.Add("🤖 ...");

            string aiResponse;

            if (message.Contains("요약"))
            {
                aiResponse = await _geminiService.GetScheduleSummaryAsync(taskList, message);
            }
            else if (message.Contains("조언"))
            {
                aiResponse = await _geminiService.GetScheduleSummaryAsync(taskList, message);
            }
            else
            {
                aiResponse = await _geminiService.GetResponseAsync(message);
            }

            ChatList.Items.RemoveAt(ChatList.Items.Count - 1); // "..." 제거
            ChatList.Items.Add("🤖 " + aiResponse);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UserInputBox.Text))
            {
                SendMessageAsync(UserInputBox.Text.Trim());
                UserInputBox.Clear();
            }
        }

        private void ExampleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SendMessageAsync(btn.Content.ToString());
            }
        }
    }

}
