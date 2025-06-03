using PlanMate.Models;
using PlanMate.ViewModels;
using PlanMate.Views;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace PlanMate;
    
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<TaskItem> taskList = new();
    public string CurrentDate => DateTime.Now.ToString("yyyy년 M월 d일 (ddd)", new CultureInfo("ko-KR"));
    public ICommand DeleteTaskCommand { get; }
    private readonly string savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "tasks.json");
    string memoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "memo.json");
    private readonly string scheduleSavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "schedules.json");
    private DateTime currentMonth = DateTime.Today;
    private Border? selectedBorder = null;
    private DateTime selectedDate = DateTime.Today; // 🔹 기본 선택: 오늘
    private MainViewModel viewModel;

    // 시간표 드래그용 필드
    private Point _dragStartPoint;
    private bool _isDragging;
    private Rectangle _newRectPreview;

    public MainWindow()
    {
        InitializeComponent();

        // ViewModel 생성 및 바인딩
        viewModel = new MainViewModel();
        DataContext = viewModel; // MainWindow가 DataContext, 내부에서 ViewModel 노출

        DeleteTaskCommand = new RelayCommand(DeleteTask);

        LoadTasks(); // taskList ← 로컬 ObservableCollection<TaskItem>
        DailyTaskList.ItemsSource = taskList;

        if (File.Exists(memoPath))
        {
            MemoBox.Text = File.ReadAllText(memoPath);
        }

        GenerateCalendar();

        LoadSchedules();

        Loaded += (s, e) => DrawLines();
    }

    // ViewModel 접근용 프로퍼티
    public MainViewModel ViewModel => viewModel;

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
        else if (tabIndex == 2) // 시간표
        {
            var dlgVm = new ScheduleDialogViewModel(timeLabels: viewModel.TimeLabels);
            var dlg = new ScheduleDialog(dlgVm) { Owner = this };

            bool? result = dlg.ShowDialog();
            if (result == true && dlgVm.NewItem != null)
            {
                viewModel.ScheduleItems.Add(dlgVm.NewItem);
                SaveSchedules();
            }
        }
        else if (tabIndex == 3) // 메모
        {

        }
    }
    #region ai 관련 코드
    private void AiButton_Click(object sender, RoutedEventArgs e)
    {
        // taskList는 현재 프로그램에서 관리 중인 일정 리스트 (예: ObservableCollection<TaskItem>)
        var taskListForAi = taskList.ToList(); // List<TaskItem>로 변환
        var chatWindow = new AiChatWindow(taskListForAi);
        chatWindow.Show();
    }

    #endregion

    #region 일간, 월간 관련 코드
    private void DeleteTask(object obj)
    {
        if (obj is TaskItem task)
        {
            if (MessageBox.Show($"{task.Name} 일정을 삭제하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                taskList.Remove(task);
                SaveTasks(); // JsonStorageService.SaveTasks(taskList);
                RefreshTaskList(); // 화면 반영
                GenerateCalendar();
            }
        }
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

                foreach (var task in tasksOnDate.Take(5))
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

                        var dayWindow = new DayTaskWindow(currentDate, taskList, RefreshTaskList, GenerateCalendar);
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
                GenerateCalendar();
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

    #endregion


    #region 시간표 관련 코드
    // 요일, 시간마다 구분선 그리기
    private void DrawLines()
    {
        double canvasWidth = ScheduleCanvas.Width;   // 7일 * 50px = 350
        double canvasHeight = ScheduleCanvas.Height;  // 24h * 60px = 1440
        double hourHeight = 60.0;  // 1시간 = 60px
        double columnWidth = 50.0;  // 1일  = 50px

        // 시간마다 가로선
        for (int hour = 1; hour < 24; hour++)
        {
            double y = hour * hourHeight;
            var hLine = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = canvasWidth,
                Y2 = y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            // 일정 아이템들보다 뒤에 배치
            ScheduleCanvas.Children.Insert(0, hLine);
        }

        // 요일마다 세로선
        for (int day = 1; day < 7; day++)
        {
            double x = day * columnWidth;
            var vLine = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = canvasHeight,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            ScheduleCanvas.Children.Insert(0, vLine);
        }
    }

    // 시간표 스크롤 동기화
    private void ScheduleScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // 수직 스크롤: 시간축에 동기화
        TimeScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);

        // 수평 스크롤: 요일 헤더에 동기화
        HeaderScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    // 드래그 시작 (일정 추가용)
    private void ScheduleScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var canvasPos = e.GetPosition(ScheduleCanvas);
        if (canvasPos.X < 0 || canvasPos.Y < 0 ||
            canvasPos.X > ScheduleCanvas.Width || canvasPos.Y > ScheduleCanvas.Height)
            return;

        _dragStartPoint = canvasPos;
        _isDragging = true;

        _newRectPreview = new Rectangle
        {
            Stroke = Brushes.Gray,
            StrokeDashArray = new DoubleCollection { 3, 3 },
            Fill = new SolidColorBrush(Color.FromArgb(50, 135, 206, 250)),
            Width = 50, // 요일 너비 50px
            Height = 0
        };
        int dayIndex = (int)(canvasPos.X / 50);
        Canvas.SetLeft(_newRectPreview, dayIndex * 50);
        Canvas.SetTop(_newRectPreview, _dragStartPoint.Y);
        ScheduleCanvas.Children.Add(_newRectPreview);
    }

    // 드래그 중 실시간 미리보기
    private void ScheduleScrollViewer_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _newRectPreview == null) return;

        var currentPos = e.GetPosition(ScheduleCanvas);
        double top = Math.Min(_dragStartPoint.Y, currentPos.Y);
        double height = Math.Abs(currentPos.Y - _dragStartPoint.Y);

        Canvas.SetTop(_newRectPreview, top);
        _newRectPreview.Height = height;
    }

    // 드래그 완료 → 일정 추가 다이얼로그 열기
    private void ScheduleScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || _newRectPreview == null) return;
        _isDragging = false;

        try
        {
            var endPoint = e.GetPosition(ScheduleCanvas);
            int dayIndex = (int)(_dragStartPoint.X / 50);
            if (dayIndex < 0) dayIndex = 0;
            if (dayIndex > 6) dayIndex = 6;

            double y1 = Math.Min(_dragStartPoint.Y, endPoint.Y);
            double y2 = Math.Max(_dragStartPoint.Y, endPoint.Y);

            var st = TimeSpan.FromMinutes(y1);
            var et = TimeSpan.FromMinutes(y2);

            var dlgVm = new ScheduleDialogViewModel(existingItem: null, timeLabels: viewModel.TimeLabels);
            dlgVm.Day = (DayOfWeek)dayIndex;
            dlgVm.StartTimeString = $"{st.Hours:00}:{st.Minutes:00}";
            dlgVm.EndTimeString = $"{et.Hours:00}:{et.Minutes:00}";

            var dlg = new ScheduleDialog(dlgVm) { Owner = this };
            bool? result = dlg.ShowDialog();
            if (result == true && dlgVm.NewItem != null)
            {
                viewModel.ScheduleItems.Add(dlgVm.NewItem);
            }
        }
        finally
        {
            if (_newRectPreview != null)
            {
                ScheduleCanvas.Children.Remove(_newRectPreview);
                _newRectPreview = null;
            }
        }
    }

    // 블록 클릭 → 편집/삭제 다이얼로그
    private void ScheduleBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_newRectPreview != null)
        {
            ScheduleCanvas.Children.Remove(_newRectPreview);
            _newRectPreview = null;
            _isDragging = false;
        }

        if (!(sender is FrameworkElement fe) || !(fe.DataContext is ScheduleItem item))
            return;

        var dlgVm = new ScheduleDialogViewModel(item, viewModel.TimeLabels);
        var dlg = new ScheduleDialog(dlgVm) { Owner = this };

        bool? result = dlg.ShowDialog();

        if (result == false && dlgVm.RequestDelete)
        {
            viewModel.ScheduleItems.Remove(item);
            SaveSchedules();
            return;
        }

        if (result == true && !dlgVm.RequestDelete)
        {
            SaveSchedules();
        }
    }

    private void SaveSchedules()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(scheduleSavePath)!);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(viewModel.ScheduleItems, options);
            File.WriteAllText(scheduleSavePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"스케줄 저장 오류: {ex.Message}");
        }
    }

    private void LoadSchedules()
    {
        try
        {
            if (!File.Exists(scheduleSavePath))
                return;

            string json = File.ReadAllText(scheduleSavePath).Trim();

            if (string.IsNullOrEmpty(json))
                return;

            var loaded = JsonSerializer.Deserialize<ObservableCollection<ScheduleItem>>(json);

            if (loaded == null || loaded.Count == 0)
                return;

            viewModel.ScheduleItems.Clear();
            foreach (var item in loaded)
                viewModel.ScheduleItems.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"스케줄 불러오기 오류: {ex.Message}");
        }
    }

    #endregion
}