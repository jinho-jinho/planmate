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
            DayTaskListBox.ItemsSource = tasks;
        }

        private void SortByImportance_Click(object sender, RoutedEventArgs e)
        {
            var sorted = originalTasks.OrderBy(t =>
                t.Importance == "상" ? 0 :
                t.Importance == "중" ? 1 : 2).ToList();
            DayTaskListBox.ItemsSource = sorted;
        }

        private void SortByTime_Click(object sender, RoutedEventArgs e)
        {
            var sorted = originalTasks.OrderBy(t => t.StartTime).ToList();
            DayTaskListBox.ItemsSource = sorted;
        }
    }
}
