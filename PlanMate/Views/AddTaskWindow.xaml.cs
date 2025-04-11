using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            if (sender is Button btn && btn.Tag is string tag)
            {
                selectedImportance = tag;

                foreach (var child in ImportancePanel.Children)
                {
                    if (child is Button b)
                    {
                        b.BorderThickness = new Thickness(1);
                    }
                }

                btn.BorderThickness = new Thickness(3);
                btn.BorderBrush = Brushes.Blue;
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
        private void TodayOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StartDatePicker.IsEnabled = false;
            EndDatePicker.IsEnabled = false;
        }
        private void TodayOnlyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StartDatePicker.IsEnabled = true;
            EndDatePicker.IsEnabled = true;
        }
        private void AllDay_Checked(object sender, RoutedEventArgs e)
        {
            StartTimeBox.IsEnabled = false;
            EndTimeBox.IsEnabled = false;
        }
        private void AllDay_Unchecked(object sender, RoutedEventArgs e)
        {
            StartTimeBox.IsEnabled = true;
            EndTimeBox.IsEnabled = true;
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

                string startInput = "0000";
                string endInput = "2359";

                int startHour = 0;
                int startMin = 0;
                int endHour = 23;
                int endMin = 59;

                if (AllDay.IsChecked != true)
                {
                    // 시간 전처리: "00:00" → "0000"
                    startInput = StartTimeBox.Text.Replace(":", "").Trim();
                    endInput = EndTimeBox.Text.Replace(":", "").Trim();

                    if (!int.TryParse(startInput, out int startRaw) || startInput.Length != 4)
                    {
                        MessageBox.Show("시작 시간을 '0000' 또는 '00:00' 형식으로 입력하세요.");
                        return;
                    }

                    if (!int.TryParse(endInput, out int endRaw) || endInput.Length != 4)
                    {
                        MessageBox.Show("종료 시간을 '0000' 또는 '00:00' 형식으로 입력하세요.");
                        return;
                    }

                    startHour = int.Parse(startInput.Substring(0, 2));
                    startMin = int.Parse(startInput.Substring(2, 2));
                    endHour = int.Parse(endInput.Substring(0, 2));
                    endMin = int.Parse(endInput.Substring(2, 2));

                    if (startHour > 23 || startMin > 59 || endHour > 23 || endMin > 59)
                    {
                        MessageBox.Show("시간은 0000~2359 사이여야 합니다.");
                        return;
                    }
                }

                var today = DateTime.Today;
                DateTime startDate = TodayOnlyCheckBox.IsChecked == true ? today : StartDatePicker.SelectedDate ?? DateTime.MinValue;
                DateTime endDate = TodayOnlyCheckBox.IsChecked == true ? today : EndDatePicker.SelectedDate ?? DateTime.MinValue;

                if (startDate == DateTime.MinValue || endDate == DateTime.MinValue)
                {
                    MessageBox.Show("시작일과 종료일을 선택하세요.");
                    return;
                }

                if ((startDate > endDate) || (startDate == endDate && (startHour * 60 + startMin) > (endHour * 60 + endMin)))
                {
                    MessageBox.Show("시작 시간이 종료 시간보다 늦을 수 없습니다.");
                    return;
                }

                CreatedTask = new TaskItem
                {
                    Name = name,
                    StartDate = startDate,
                    EndDate = endDate,
                    StartTime = startInput,
                    EndTime = endInput,
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
