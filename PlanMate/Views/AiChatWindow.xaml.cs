using Newtonsoft.Json;
using PlanMate.Models;
using PlanMate.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PlanMate.Views
{
    public partial class AiChatWindow : Window
    {
        private readonly GeminiApiService _geminiService;
        private readonly ObservableCollection<TaskItem> taskList;
        private readonly ObservableCollection<MemoItem> memoList;
        private readonly ObservableCollection<ScheduleItem> scheduleList;

        private readonly Action? saveTasksCallback;
        private readonly Action? saveMemosCallback;
        private readonly Action? saveSchedulesCallback;
        private readonly Action? refreshUICallback;

        private ObservableCollection<ChatMessage> chatMessages = new();

        public AiChatWindow(
            ObservableCollection<TaskItem> taskItems,
            ObservableCollection<MemoItem> memos,
            ObservableCollection<ScheduleItem> schedules,
            Action? saveTasks = null,
            Action? saveMemos = null,
            Action? saveSchedules = null,
            Action? refreshUI = null)
        {
            InitializeComponent();
            _geminiService = new GeminiApiService();
            taskList = taskItems;
            memoList = memos;
            scheduleList = schedules;
            saveTasksCallback = saveTasks;
            saveMemosCallback = saveMemos;
            saveSchedulesCallback = saveSchedules;
            refreshUICallback = refreshUI;

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

            bool isScheduleCreate = Regex.IsMatch(message, @"일정.*(만들|생성|추가|등록)");
            bool isMemoCreate = Regex.IsMatch(message, @"메모.*(만들|생성|작성|추가)");
            bool isTimetableCreate = Regex.IsMatch(message, @"(시간표|수업).*(만들|생성|추가)");

            int count = Convert.ToInt32(isScheduleCreate) + Convert.ToInt32(isMemoCreate) + Convert.ToInt32(isTimetableCreate);
            if (count > 1)
            {
                aiResponse = "❗ 한 번에 하나의 항목만 생성할 수 있어요. 일정/메모/시간표 중 하나만 요청해 주세요.";
            }
            else if (isScheduleCreate)
            {
                aiResponse = await _geminiService.GenerateTaskFromMessageAsync(message);
                try
                {
                    aiResponse = ExtractPureJson(aiResponse);
                    var task = JsonConvert.DeserializeObject<TaskItem>(aiResponse);
                    if (task != null)
                    {
                        taskList.Add(task);
                        saveTasksCallback?.Invoke();
                        refreshUICallback?.Invoke();
                        aiResponse = $"✅ 일정이 생성되었습니다: **{task.Name}**";
                    }
                    else
                    {
                        aiResponse = "❌ 일정 생성 실패: 응답 파싱 불가.";
                    }
                }
                catch (Exception ex)
                {
                    aiResponse = $"❌ JSON 파싱 오류: {ex.Message}\n응답: {aiResponse}";
                }
            }
            else if (isMemoCreate)
            {
                aiResponse = await _geminiService.GenerateMemoFromMessageAsync(message);
                try
                {
                    aiResponse = ExtractPureJson(aiResponse);
                    var memo = JsonConvert.DeserializeObject<MemoItem>(aiResponse);
                    if (memo != null)
                    {
                        memoList.Add(memo);
                        saveMemosCallback?.Invoke();
                        refreshUICallback?.Invoke();
                        aiResponse = $"✅ 메모가 생성되었습니다: **{memo.Title}**";
                    }
                    else
                    {
                        aiResponse = "❌ 메모 생성 실패: 응답 파싱 불가.";
                    }
                }
                catch (Exception ex)
                {
                    aiResponse = $"❌ JSON 파싱 오류: {ex.Message}\n응답: {aiResponse}";
                }
            }
            else if (isTimetableCreate)
            {
                aiResponse = await _geminiService.GenerateScheduleFromMessageAsync(message);
                try
                {
                    aiResponse = ExtractPureJson(aiResponse);
                    var schedule = JsonConvert.DeserializeObject<ScheduleItem>(aiResponse);
                    if (schedule != null)
                    {
                        scheduleList.Add(schedule);
                        saveSchedulesCallback?.Invoke();
                        refreshUICallback?.Invoke();
                        aiResponse = $"✅ 시간표가 생성되었습니다: **{schedule.Title}** ({schedule.Day} {schedule.StartTime}-{schedule.EndTime})";
                    }
                    else
                    {
                        aiResponse = "❌ 시간표 생성 실패: 응답 파싱 불가.";
                    }
                }
                catch (Exception ex)
                {
                    aiResponse = $"❌ JSON 파싱 오류: {ex.Message}\n응답: {aiResponse}";
                }
            }
            else if (message.Contains("일정") || message.Contains("시간표") || message.Contains("메모"))
            {
                aiResponse = await _geminiService.GetSmartSummaryAsync(
                    taskList.ToList(),
                    memoList.ToList(),
                    scheduleList.ToList(),
                    message);
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
        private string ExtractPureJson(string raw)
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start >= 0 && end >= start)
                return raw.Substring(start, end - start + 1).Trim();

            return raw; // JSON이 없으면 원문 그대로 반환
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
                scrollViewer?.ScrollToBottom();
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
