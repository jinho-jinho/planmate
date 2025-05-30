using PlanMate.Models;
using PlanMate.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace PlanMate.Views
{
    public partial class DayTaskWindow : Window
    {
        private List<TaskItem> originalTasks;
        private ObservableCollection<TaskItem> sharedTaskList;
        public ICommand DeleteTaskCommand { get; }
        private DateTime SelectedDate;
        private Action refreshCallback;
        private Action calendarCallback;

        public DayTaskWindow(DateTime date, ObservableCollection<TaskItem> tasks,
                             Action refreshAction, Action calendarAction)
        {
            InitializeComponent();

            DeleteTaskCommand = new RelayCommand(obj =>
            {
                if (obj is TaskItem task)
                    DeleteTask(task);
            });

            DataContext = this;

            sharedTaskList = tasks;
            refreshCallback = refreshAction;
            calendarCallback = calendarAction;

            SelectedDate = date;
            DateTitle.Text = date.ToString("yyyy년 M월 d일 (ddd)", new CultureInfo("ko-KR"));

            var dayTasks = tasks.Where(t => t.StartDate.Date <= date && t.EndDate.Date >= date).ToList();
            DayTaskListBox.ItemsSource = dayTasks;
            originalTasks = new List<TaskItem>(dayTasks);
        }

        private void DeleteTask(TaskItem task)
        {
            if (MessageBox.Show($"{task.Name} 일정을 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                sharedTaskList.Remove(task);
                SaveTasks();
                refreshCallback?.Invoke();
                calendarCallback?.Invoke();

                var dayTasks = sharedTaskList
                    .Where(t => t.StartDate.Date <= SelectedDate && t.EndDate.Date >= SelectedDate)
                    .ToList();

                DayTaskListBox.ItemsSource = null;
                DayTaskListBox.ItemsSource = dayTasks;
                originalTasks = new List<TaskItem>(dayTasks);
            }
        }

        private void SaveTasks()
        {
            try
            {
                var savePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "tasks.json");

                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                var json = JsonSerializer.Serialize(sharedTaskList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(savePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 오류: {ex.Message}");
            }
        }

        private void RefreshList()
        {
            DayTaskListBox.ItemsSource = null;
            DayTaskListBox.ItemsSource = new List<TaskItem>(originalTasks);
        }

        private void DayTaskListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DayTaskListBox.SelectedItem is TaskItem selectedTask)
            {
                var editWindow = new AddTaskWindow(selectedTask);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshList();
                    SaveTasks();
                }

                DayTaskListBox.SelectedItem = null;
            }
        }

        private void SortByImportanceThenEndDate_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var sorted = originalTasks.OrderBy(t => t.Importance == "상" ? 0 : t.Importance == "중" ? 1 : 2)
                                       .ThenBy(t => (t.EndDate - today).Days).ToList();
            UpdateDayList(sorted);
        }

        private void SortByStartDateThenImportance_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var sorted = originalTasks.OrderBy(t => (t.StartDate - today).Days)
                                       .ThenBy(t => t.Importance == "상" ? 0 : t.Importance == "중" ? 1 : 2).ToList();
            UpdateDayList(sorted);
        }

        private void SortByEndDateThenImportance_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var sorted = originalTasks.OrderBy(t => (t.EndDate - today).Days)
                                       .ThenBy(t => t.Importance == "상" ? 0 : t.Importance == "중" ? 1 : 2).ToList();
            UpdateDayList(sorted);
        }

        private void UpdateDayList(List<TaskItem> sorted)
        {
            DayTaskListBox.ItemsSource = null;
            DayTaskListBox.ItemsSource = sorted;
        }
    }
}
