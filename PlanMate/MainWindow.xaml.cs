using PlanMate.Models;
using PlanMate.ViewModels;
using PlanMate.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PlanMate;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<TaskItem> taskList = new();
    private readonly string savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "tasks.json");
    string memoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "memo.json");
    private DateTime currentMonth = DateTime.Today;
    private Border? selectedBorder = null;
    private DateTime selectedDate = DateTime.Today; // 🔹 기본 선택: 오늘

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        LoadTasks();
        DailyTaskList.ItemsSource = taskList;
        if (File.Exists(memoPath))
        {
            MemoBox.Text = File.ReadAllText(memoPath);
        }
        GenerateCalendar();

    }

    private void OpenMonthPickerButton_Click(object sender, RoutedEventArgs e)
    {
        MonthCalendar.DisplayDate = currentMonth;
        MonthCalendar.DisplayMode = CalendarMode.Year;
        MonthPopup.IsOpen = true;
    }

    private void MonthCalendar_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
    {
        if (MonthCalendar.DisplayMode == CalendarMode.Month)
        {
            var selected = MonthCalendar.DisplayDate;
            currentMonth = new DateTime(selected.Year, selected.Month, 1);
            GenerateCalendar();
            MonthPopup.IsOpen = false; // 팝업 닫기
        }
    }


    private void GenerateCalendar()
    {
        CalendarGrid.Children.Clear();
        MonthTitle.Text = currentMonth.ToString("yyyy년 M월");

        var firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        int startOffset = (int)firstDay.DayOfWeek;

        for (int i = 0; i < 42; i++)
        {
            DateTime? date = null;
            if (i >= startOffset && i < startOffset + daysInMonth)
                date = firstDay.AddDays(i - startOffset);

            var border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Margin = new Thickness(1),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand
            };

            var stack = new StackPanel();

            if (date != null)
            {
                DateTime currentDate = date.Value;

                // 선택된 날짜 강조
                if (currentDate.Date == selectedDate.Date)
                {
                    border.BorderBrush = Brushes.DeepSkyBlue;
                    border.BorderThickness = new Thickness(2);
                    selectedBorder = border;
                }

                // 날짜 텍스트
                var dateText = new TextBlock
                {
                    Text = currentDate.Day.ToString(),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(2),
                    IsHitTestVisible = false
                };
                stack.Children.Add(dateText);

                // 할 일 필터링
                var tasksOnDate = taskList.Where(t =>
                    t.StartDate.Date <= currentDate.Date &&
                    t.EndDate.Date >= currentDate.Date).ToList();

                foreach (var task in tasksOnDate.Take(2))
                {
                    var taskText = new Border
                    {
                        CornerRadius = new CornerRadius(3),
                        Background = task.Importance == "상" ? Brushes.IndianRed :
                                     task.Importance == "중" ? Brushes.Orange :
                                                               Brushes.LightGreen,
                        Margin = new Thickness(0, 1, 0, 0),
                        Padding = new Thickness(1),
                        IsHitTestVisible = false,
                        Child = new TextBlock
                        {
                            Text = task.Name,
                            FontSize = 8,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            Foreground = Brushes.White,
                            IsHitTestVisible = false
                        }
                    };
                    stack.Children.Add(taskText);
                }

                // 셀 클릭 이벤트
                border.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    selectedDate = currentDate;

                    if (selectedBorder != null)
                    {
                        selectedBorder.BorderBrush = Brushes.Gray;
                        selectedBorder.BorderThickness = new Thickness(0.5);
                    }

                    border.BorderBrush = Brushes.DeepSkyBlue;
                    border.BorderThickness = new Thickness(2);
                    selectedBorder = border;
                };

                border.MouseLeftButtonDown += (s, e) => 
                {
                    if (e.ClickCount == 2)
                    {
                        var tasks = taskList.Where(t =>
                            t.StartDate.Date <= currentDate &&
                            t.EndDate.Date >= currentDate).ToList();

                        var dayWindow = new DayTaskWindow(currentDate, tasks);
                        dayWindow.Show();
                    }
                };

            }

            border.Child = stack;
            CalendarGrid.Children.Add(border);
        }

    }


    private void OnPrevMonthClick(object sender, RoutedEventArgs e)
    {
        currentMonth = currentMonth.AddMonths(-1);
        GenerateCalendar();
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        currentMonth = currentMonth.AddMonths(1);
        GenerateCalendar();
    }


    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        int tabIndex = MainTab.SelectedIndex; // 0: 일간, 1: 월간, 2: 시간표, 3: 메모

        if (tabIndex == 0 || tabIndex == 1) // 일간, 월간
        {
            var addWindow = new AddTaskWindow();
            if (addWindow.ShowDialog() == true)
            {
                taskList.Add(addWindow.CreatedTask);
                RefreshTaskList();
                SaveTasks();
                GenerateCalendar(); // 월간 캘린더 갱신
            }
        }
        else if(tabIndex == 2) // 시간표
        {
            
        }
        else if (tabIndex == 3) // 메모
        {
            
        }
    }


    private void LoadTasks()
    {
        try
        {
            if (File.Exists(savePath))
            {
                var json = File.ReadAllText(savePath);
                var loaded = JsonSerializer.Deserialize<ObservableCollection<TaskItem>>(json);
                if (loaded != null)
                    taskList = loaded;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"불러오기 오류: {ex.Message}");
        }
    }

    private void SaveTasks()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            var json = JsonSerializer.Serialize(taskList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(savePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"저장 오류: {ex.Message}");
        }
    }
    private void DailyTaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DailyTaskList.SelectedItem is TaskItem selectedTask)
        {
            var editWindow = new AddTaskWindow(selectedTask);  // 기존 Task 전달
            if (editWindow.ShowDialog() == true)
            {
                // 변경사항은 이미 바인딩된 TaskItem에 반영됨
                RefreshTaskList();
                SaveTasks();
            }

            DailyTaskList.SelectedItem = null; // 다시 클릭 가능하도록 선택 해제
        }
    }
    private void RefreshTaskList()
    {
        // 기본: 중요도 > 종료일 기준
        SortByImportanceThenDDay_Click(null, null);
    }

    private void SortByImportanceThenDDay_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;

        var sorted = taskList.OrderBy(t =>
            t.Importance == "상" ? 0 :
            t.Importance == "중" ? 1 : 2
        ).ThenBy(t => (t.EndDate - today).Days).ToList();

        UpdateTaskList(sorted);
    }

    private void SortByStartDateThenImportance_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;

        var sorted = taskList.OrderBy(t => (t.StartDate - today).Days)
            .ThenBy(t =>
                t.Importance == "상" ? 0 :
                t.Importance == "중" ? 1 : 2
            ).ToList();

        UpdateTaskList(sorted);
    }

    private void SortByEndDateThenImportance_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;

        var sorted = taskList.OrderBy(t => (t.EndDate - today).Days)
            .ThenBy(t =>
                t.Importance == "상" ? 0 :
                t.Importance == "중" ? 1 : 2
            ).ToList();

        UpdateTaskList(sorted);
    }

    private void UpdateTaskList(List<TaskItem> sorted)
    {
        taskList.Clear();
        foreach (var t in sorted)
            taskList.Add(t);
    }




    private void MemoBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(memoPath)!);
            File.WriteAllText(memoPath, MemoBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show("메모 저장 실패: " + ex.Message);
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        this.Opacity = e.NewValue;
    }
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}
