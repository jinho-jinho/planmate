using PlanMate.Models;
using PlanMate.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PlanMate.Views
{
    public partial class AiChatWindow : Window
    {
        private readonly GeminiApiService _geminiService;
        private readonly List<TaskItem> taskList;
        private readonly List<MemoItem> memoList;
        private readonly List<ScheduleItem> scheduleList;

        private ObservableCollection<ChatMessage> chatMessages = new();

        public AiChatWindow(List<TaskItem> taskItems, List<MemoItem> memos, List<ScheduleItem> schedules)
        {
            InitializeComponent();
            _geminiService = new GeminiApiService();
            taskList = taskItems;
            memoList = memos;
            scheduleList = schedules;

            ChatList.ItemsSource = chatMessages;
        }

        private async void SendMessageAsync(string message)
        {
            if (EmptyHintPanel.Visibility == Visibility.Visible)
            {
                EmptyHintPanel.Visibility = Visibility.Collapsed;
                ChatList.Visibility = Visibility.Visible;
            }

            UserInputBox.IsEnabled = false;
            SendButton.IsEnabled = false;

            chatMessages.Add(new ChatMessage { Role = "User", Message = message });
            chatMessages.Add(new ChatMessage { Role = "Bot", Message = "..." });
            ScrollToBottom();

            string aiResponse;

            // 🔍 키워드 포함 여부에 따라 조건 분기
            if (message.Contains("일정") || message.Contains("시간표") || message.Contains("메모") ||
                message.Contains("요약") || message.Contains("조언"))
            {
                aiResponse = await _geminiService.GetSmartSummaryAsync(taskList, memoList, scheduleList, message);
            }
            else
            {
                aiResponse = await _geminiService.GetResponseAsync(message);
            }

            chatMessages.RemoveAt(chatMessages.Count - 1);
            chatMessages.Add(new ChatMessage { Role = "Bot", Message = aiResponse });
            ScrollToBottom();

            UserInputBox.IsEnabled = true;
            SendButton.IsEnabled = true;
            UserInputBox.Focus();
        }

        private void UserInputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && SendButton.IsEnabled)
            {
                SendButton_Click(null, null);
                e.Handled = true;
            }
        }

        private void ScrollToBottom()
        {
            Dispatcher.InvokeAsync(() =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(ChatList);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToBottom();
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                T? result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
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
