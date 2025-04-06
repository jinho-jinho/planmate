using System;
using System.Windows;
using PlanMate.Models;

namespace PlanMate.Views
{
    public partial class AddTaskWindow : Window
    {
        public TaskItem CreatedTask { get; private set; }
        private string selectedImportance = "중"; // 기본값

        public AddTaskWindow()
        {
            InitializeComponent();
        }

        private void Importance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement btn && btn.Tag is string tag)
            {
                selectedImportance = tag;
            }
        }

        private void StartTimeBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (StartTimeBox.Text == "00:00") StartTimeBox.Text = "";
        }

        private void EndTimeBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EndTimeBox.Text == "00:00") EndTimeBox.Text = "";
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = TaskNameBox.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("할 일 이름을 입력하세요.");
                    return;
                }

                var today = DateTime.Today;
                DateTime startDate = TodayOnlyCheckBox.IsChecked == true ? today : StartDatePicker.SelectedDate ?? today;
                DateTime endDate = TodayOnlyCheckBox.IsChecked == true ? today : EndDatePicker.SelectedDate ?? today;

                TimeSpan startTime = TimeSpan.Parse(StartTimeBox.Text);
                TimeSpan endTime = TimeSpan.Parse(EndTimeBox.Text);

                CreatedTask = new TaskItem
                {
                    Name = name,
                    StartDate = startDate,
                    EndDate = endDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Importance = selectedImportance,
                    Details = DetailBox.Text,
                    IsCompleted = false
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류: {ex.Message}");
            }
        }
    }
}
