using PlanMate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PlanMate.Views
{
    public partial class DayTaskWindow : Window
    {
        private List<TaskItem> originalTasks;

        public DayTaskWindow(DateTime date, List<TaskItem> tasks)
        {
            InitializeComponent();
            DateTitle.Text = date.ToString("yyyy년 M월 d일 (ddd)", new System.Globalization.CultureInfo("ko-KR"));
            originalTasks = tasks;
            DayTaskListBox.ItemsSource = new List<TaskItem>(tasks); // 초기 정렬 없이 표시
        }
        private void DayTaskListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DayTaskListBox.SelectedItem is TaskItem selectedTask)
            {
                var editWindow = new AddTaskWindow(selectedTask);  // 기존 Task 전달
                if (editWindow.ShowDialog() == true)
                {
                    // 변경사항은 originalTasks에서 이미 반영됨
                    DayTaskListBox.ItemsSource = null;
                    DayTaskListBox.ItemsSource = new List<TaskItem>(originalTasks);
                }

                DayTaskListBox.SelectedItem = null; // 다시 클릭 가능하도록 해제
            }
        }
        private void SortByImportanceThenEndDate_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;

            var sorted = originalTasks.OrderBy(t =>
                t.Importance == "상" ? 0 :
                t.Importance == "중" ? 1 : 2
            ).ThenBy(t => (t.EndDate - today).Days).ToList();

            UpdateDayList(sorted);
        }

        private void SortByStartDateThenImportance_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;

            var sorted = originalTasks.OrderBy(t => (t.StartDate - today).Days)
                .ThenBy(t =>
                    t.Importance == "상" ? 0 :
                    t.Importance == "중" ? 1 : 2
                ).ToList();

            UpdateDayList(sorted);
        }

        private void SortByEndDateThenImportance_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;

            var sorted = originalTasks.OrderBy(t => (t.EndDate - today).Days)
                .ThenBy(t =>
                    t.Importance == "상" ? 0 :
                    t.Importance == "중" ? 1 : 2
                ).ToList();

            UpdateDayList(sorted);
        }

        private void UpdateDayList(List<TaskItem> sorted)
        {
            DayTaskListBox.ItemsSource = null;
            DayTaskListBox.ItemsSource = sorted;
        }
    }
}
