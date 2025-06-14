using Microsoft.Win32;
using PlanMate.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlanMate.Views
{
    public partial class AddTaskWindow : Window
    {
        public TaskItem CreatedTask { get; private set; }
        private string selectedImportance = "중";
        private List<string> relatedDocs = new();
        private bool isEditMode;

        public AddTaskWindow()
        {
            InitializeComponent();
            CreatedTask = new TaskItem(); // 신규 Task
            DataContext = CreatedTask;
            isEditMode = false;
        }
        public AddTaskWindow(TaskItem existingTask)
        {
            InitializeComponent();
            CreatedTask = existingTask;
            DataContext = CreatedTask;
            isEditMode = true;
        }
        public AddTaskWindow(DateTime defaultDate)
        {
            InitializeComponent();
            CreatedTask = new TaskItem
            {
                StartDate = defaultDate,
                EndDate = defaultDate
            };
            DataContext = CreatedTask;
            isEditMode = false;
        }


        // 🔹 관련 문서 추가 (탐색기에서 선택)
        private void AddDoc_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "관련 문서 선택",
                Filter = "모든 파일 (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;

                if (!relatedDocs.Contains(path))
                {
                    relatedDocs.Add(path);
                    UpdateDocListBox();
                }
                else
                {
                    MessageBox.Show("이미 추가된 문서입니다.");
                }
            }
        }


        // 🔹 관련 문서 삭제
        private void RemoveDoc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                relatedDocs.Remove(path);
                UpdateDocListBox();
            }
        }


        // 🔹 파일 이름만 보여주는 리스트 갱신
        private void UpdateDocListBox()
        {
            var displayList = new List<DocDisplayItem>();
            foreach (var path in relatedDocs)
            {
                displayList.Add(new DocDisplayItem
                {
                    FileName = Path.GetFileName(path),
                    FullPath = path
                });
            }
            DocListBox.ItemsSource = null;
            DocListBox.ItemsSource = displayList;
        }


        private class DocDisplayItem
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }
        }

        private void Importance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                selectedImportance = tag;
                foreach (var child in ImportancePanel.Children)
                {
                    if (child is Button b)
                        b.BorderThickness = new Thickness(1);
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
                int startHour = 0, startMin = 0, endHour = 23, endMin = 59;

                if (AllDay.IsChecked != true)
                {
                    startInput = StartTimeBox.Text.Replace(":", "").Trim();
                    endInput = EndTimeBox.Text.Replace(":", "").Trim();

                    if (!int.TryParse(startInput, out _) || startInput.Length != 4 ||
                        !int.TryParse(endInput, out _) || endInput.Length != 4)
                    {
                        MessageBox.Show("시작/종료 시간 형식이 잘못되었습니다.");
                        return;
                    }

                    startHour = int.Parse(startInput.Substring(0, 2));
                    startMin = int.Parse(startInput.Substring(2, 2));
                    endHour = int.Parse(endInput.Substring(0, 2));
                    endMin = int.Parse(endInput.Substring(2, 2));

                    if (startHour > 23 || startMin > 59 || endHour > 23 || endMin > 59)
                    {
                        MessageBox.Show("시간은 0000~2359 범위여야 합니다.");
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

                if (!isEditMode)
                {
                    CreatedTask = new TaskItem();
                }

                CreatedTask.Name = name;
                CreatedTask.StartDate = startDate;
                CreatedTask.EndDate = endDate;
                CreatedTask.StartTime = startInput;
                CreatedTask.EndTime = endInput;
                CreatedTask.Importance = selectedImportance;
                CreatedTask.Details = DetailBox.Text;
                CreatedTask.IsCompleted = false;
                CreatedTask.RelatedDocs = new List<string>(relatedDocs);

                DialogResult = true;
                Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류: {ex.Message}");
            }
        }
        private void OpenDoc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("파일을 찾을 수 없습니다.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("파일 열기 실패: " + ex.Message);
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (isEditMode && CreatedTask != null)
            {
                TaskNameBox.Text = CreatedTask.Name;
                DetailBox.Text = CreatedTask.Details;
                StartTimeBox.Text = CreatedTask.StartTime;
                EndTimeBox.Text = CreatedTask.EndTime;
                StartDatePicker.SelectedDate = CreatedTask.StartDate;
                EndDatePicker.SelectedDate = CreatedTask.EndDate;

                foreach (var child in ImportancePanel.Children)
                {
                    if (child is Button b)
                    {
                        b.BorderThickness = new Thickness(1);
                        if (b.Tag is string tag && tag == CreatedTask.Importance)
                        {
                            b.BorderThickness = new Thickness(3);
                            b.BorderBrush = Brushes.Blue;
                        }
                    }
                }

                selectedImportance = CreatedTask.Importance;
                relatedDocs = new List<string>(CreatedTask.RelatedDocs ?? new());
                UpdateDocListBox();
            }
            else if (!isEditMode && CreatedTask != null)
            {
                // 신규 등록 시 기본 날짜 설정 
                StartDatePicker.SelectedDate = CreatedTask.StartDate;
                EndDatePicker.SelectedDate = CreatedTask.EndDate;
            }
        }
    }
}
