using PlanMate.Models;
using PlanMate.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PlanMate.Views
{
    public partial class AiChatWindow : Window
    {
        private readonly GeminiApiService _geminiService;
        private readonly List<TaskItem> taskList;
        private ObservableCollection<ChatMessage> chatMessages = new();

        public AiChatWindow(List<TaskItem> taskItems)
        {
            InitializeComponent();
            _geminiService = new GeminiApiService();
            taskList = taskItems;

            ChatList.ItemsSource = chatMessages;
        }

        private async void SendMessageAsync(string message)
        {
            // 모든 입력 UI 잠금
            UserInputBox.IsEnabled = false;
            SendButton.IsEnabled = false;
            ExampleButtonPanel.IsEnabled = false;

            chatMessages.Add(new ChatMessage { Role = "User", Message = message });
            chatMessages.Add(new ChatMessage { Role = "Bot", Message = "..." });
            ScrollToBottom();

            string aiResponse;

            if (message.Contains("요약"))
                aiResponse = await _geminiService.GetScheduleSummaryAsync(taskList, message);
            else if (message.Contains("조언"))
                aiResponse = await _geminiService.GetScheduleSummaryAsync(taskList, message);
            else
                aiResponse = await _geminiService.GetResponseAsync(message);

            chatMessages.RemoveAt(chatMessages.Count - 1);
            chatMessages.Add(new ChatMessage { Role = "Bot", Message = aiResponse });
            ScrollToBottom();

            // 입력 UI 다시 활성화
            UserInputBox.IsEnabled = true;
            SendButton.IsEnabled = true;
            ExampleButtonPanel.IsEnabled = true;
            UserInputBox.Focus();
        }

        private void UserInputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && SendButton.IsEnabled)
            {
                SendButton_Click(null, null);
                e.Handled = true; // 엔터가 줄바꿈으로 처리되지 않도록 막기
            }
        }

        private void ScrollToBottom()
        {
            // UI가 렌더링 완료된 후에 강제로 스크롤
            Dispatcher.InvokeAsync(() =>
            {
                ChatList.UpdateLayout();
                ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        private void MessageBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock &&
                textBlock.DataContext is ChatMessage message)
            {
                ApplyBoldMarkdown(textBlock, message.Message);
            }
        }
        private void ApplyBoldMarkdown(TextBlock textBlock, string rawText) 
        {
            textBlock.Inlines.Clear();

            var parts = rawText.Split(new[] { "**" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                var run = new Run(parts[i])
                {
                    FontWeight = (i % 2 == 1) ? FontWeights.Bold : FontWeights.Normal
                };
                textBlock.Inlines.Add(run);
            }
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
