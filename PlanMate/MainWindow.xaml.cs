using PlanMate.Models;
using PlanMate.ViewModels;
using PlanMate.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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
    private readonly string scheduleSavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "schedules.json");
    private DateTime currentMonth = DateTime.Today;
    private Border? selectedBorder = null;
    private DateTime selectedDate = DateTime.Today; // 🔹 기본 선택: 오늘
    public MainViewModel viewModel { get; }
    private bool _hasScrolledToInitialTime = false;
    private bool _internalDisplayModeChange = false;
    // 시간표 드래그용 필드
    private Point _dragStartPoint;
    private bool _isDragging;
    private Rectangle _newRectPreview;
    private static readonly string SettingPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PlanMate",
    "user_settings.json");

    public class UserSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int LastSelectedTabIndex { get; set; }
        public string BackgroundColor { get; set; } = "LightGray";
    }
    private UserSettings settings = new();
    public MainWindow()
    {
        InitializeComponent();

        viewModel = new MainViewModel();  // ViewModel 생성
        DataContext = ViewModel;          // MainWindow를 루트 바인딩 객체로 사용

        LoadWindowSettings();

        DeleteTaskCommand = new RelayCommand(DeleteTask);  // 삭제 커맨드

        LoadTasks();
        DailyTaskList.ItemsSource = taskList;

        GenerateCalendar();
        LoadSchedules();

        ScheduleCanvas.SizeChanged += (s, e) =>
        {
            DrawLines();

            if (!_hasScrolledToInitialTime)
            {
                _hasScrolledToInitialTime = true;

                // 8.5시간 = 8시간 30분 -> 분 단위
                double minutes = 8.5 * 60;

                // 캔버스 전체 높이(24시간 분량)
                double totalHeight = ScheduleCanvas.ActualHeight;

                // 픽셀로 변환: minutes × totalHeight ÷ (24h*60min)
                double offset = minutes * totalHeight / (24 * 60);

                // 그 위치를 뷰포트 최상단으로
                ScheduleScrollViewer.ScrollToVerticalOffset(offset);
            }
        };

        // 🔹 UI 로드 후 실행할 작업
        Loaded += (s, e) =>
        {
            // 반드시 Dispatcher로 지연 실행
            Dispatcher.InvokeAsync(() =>
            {
                if (settings.LastSelectedTabIndex >= 0 && settings.LastSelectedTabIndex < MainTab.Items.Count)
                {
                    MainTab.SelectedIndex = settings.LastSelectedTabIndex;
                    Console.WriteLine($"[복원] 설정된 탭 인덱스: {settings.LastSelectedTabIndex}");
                }
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        };

        // 🔹 창 종료 시 사용자 설정 저장
        Closing += (s, e) => SaveWindowSettings();

        // 🔹 탭 변경 시 인덱스 저장
        MainTab.SelectionChanged += MainTab_SelectionChanged;
        if (DataContext is PlanMate.ViewModels.MainViewModel vm)
            vm.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ChangeBg_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string color)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ChangeBackground(color);
                settings.BackgroundColor = color;  // 🟢 색 이름을 그대로 저장
                SaveWindowSettings();
            }
        }
    }


    private void Resize_TopLeft(object sender, DragDeltaEventArgs e)
{
    var newWidth = Width - e.HorizontalChange;
    var newHeight = Height - e.VerticalChange;
    if (newWidth > MinWidth)
    {
        Left += e.HorizontalChange;
        Width = newWidth;
    }
    if (newHeight > MinHeight)
    {
        Top += e.VerticalChange;
        Height = newHeight;
    }
}

private void Resize_TopRight(object sender, DragDeltaEventArgs e)
{
    var newWidth = Width + e.HorizontalChange;
    var newHeight = Height - e.VerticalChange;
    if (newWidth > MinWidth)
    {
        Width = newWidth;
    }
    if (newHeight > MinHeight)
    {
        Top += e.VerticalChange;
        Height = newHeight;
    }
}

private void Resize_BottomLeft(object sender, DragDeltaEventArgs e)
{
    var newWidth = Width - e.HorizontalChange;
    var newHeight = Height + e.VerticalChange;
    if (newWidth > MinWidth)
    {
        Left += e.HorizontalChange;
        Width = newWidth;
    }
    if (newHeight > MinHeight)
    {
        Height = newHeight;
    }
}

private void Resize_BottomRight(object sender, DragDeltaEventArgs e)
{
    var newWidth = Width + e.HorizontalChange;
    var newHeight = Height + e.VerticalChange;
    if (newWidth > MinWidth) Width = newWidth;
    if (newHeight > MinHeight) Height = newHeight;
}

private void Resize_Top(object sender, DragDeltaEventArgs e)
{
    var newHeight = Height - e.VerticalChange;
    if (newHeight > MinHeight)
    {
        Top += e.VerticalChange;
        Height = newHeight;
    }
}

private void Resize_Bottom(object sender, DragDeltaEventArgs e)
{
    var newHeight = Height + e.VerticalChange;
    if (newHeight > MinHeight)
        Height = newHeight;
}

private void Resize_Left(object sender, DragDeltaEventArgs e)
{
    var newWidth = Width - e.HorizontalChange;
    if (newWidth > MinWidth)
    {
        Left += e.HorizontalChange;
        Width = newWidth;
    }
}

private void Resize_Right(object sender, DragDeltaEventArgs e)
{
    var newWidth = Width + e.HorizontalChange;
    if (newWidth > MinWidth)
        Width = newWidth;
}


// ViewModel 접근용 프로퍼티
public MainViewModel ViewModel => viewModel;
    private void LoadWindowSettings()
    {
        if (File.Exists(SettingPath))
        {
            var json = File.ReadAllText(SettingPath);
            settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();

            this.Left = settings.Left;
            this.Top = settings.Top;
            this.Width = settings.Width;
            this.Height = settings.Height;

            if (!string.IsNullOrEmpty(settings.BackgroundColor) && DataContext is MainViewModel vm)
            {
                vm.ChangeBackground(settings.BackgroundColor);
            }
        }
    }


    private void SaveWindowSettings()
    {
        settings.Left = this.Left;
        settings.Top = this.Top;
        settings.Width = this.Width;
        settings.Height = this.Height;


        Directory.CreateDirectory(Path.GetDirectoryName(SettingPath)!);
        var json = JsonSerializer.Serialize(settings);
        File.WriteAllText(SettingPath, json);
    }

    private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && MainTab.SelectedIndex >= 0)
        {
            settings.LastSelectedTabIndex = MainTab.SelectedIndex;

            SaveWindowSettings();
        }
        if (MainTab.SelectedIndex == 3)
            SetPlaceholder();
    }



    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        int tabIndex = MainTab.SelectedIndex; // 0: 일간, 1: 월간, 2: 시간표, 3: 메모

        if (tabIndex == 0 || tabIndex == 1) // 일간, 월간
        {
            var targetDate = selectedDate;  // 🔹 선택된 날짜 사용

            var addWindow = new AddTaskWindow(targetDate);  // 🔹 날짜 넘기기
            addWindow.Owner = this;

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
            var dlgVm = new ScheduleDialogViewModel(null, viewModel.ScheduleItems, timeLabels: viewModel.TimeLabels);
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
            (DataContext as PlanMate.ViewModels.MainViewModel)?.AddMemoCommand.Execute(null);
        }
    }
    #region ai 관련 코드
    private void AiButton_Click(object sender, RoutedEventArgs e)
    {
        var taskListForAi = taskList.ToList();
        var memoListForAi = ViewModel.Memos.ToList();
        var scheduleListForAi = ViewModel.ScheduleItems.ToList();

        var chatWindow = new AiChatWindow(
                taskList,                            // ✅ MainWindow의 필드 taskList
                ViewModel.Memos,                     // ✅ ViewModel의 바인딩된 메모
                ViewModel.ScheduleItems,            // ✅ ViewModel의 바인딩된 스케줄
                SaveTasks,                           // ✅ 반드시 MainWindow의 SaveTasks 메서드
                //SaveMemos,
                SaveSchedules,
                () =>
                {
                    RefreshTaskList();
                    GenerateCalendar();
                }
            )
        {
            Owner = this,
            Top = this.Top
        };

        chatWindow.Show();


        double screenWidth = SystemParameters.WorkArea.Width;
        chatWindow.Left = (this.Left + this.Width + chatWindow.Width <= screenWidth)
            ? this.Left + this.Width
            : this.Left - chatWindow.Width;

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
        if (_internalDisplayModeChange)
            return;

        if (MonthCalendar.DisplayMode == CalendarMode.Month)
        {
            var selected = MonthCalendar.DisplayDate;
            currentMonth = new DateTime(selected.Year, selected.Month, 1);
            GenerateCalendar();
            MonthPopup.IsOpen = false; // 팝업 닫기
        }
    }
    private void MonthPopup_Opened(object sender, EventArgs e)
    {
        _internalDisplayModeChange = true;

        MonthCalendar.DisplayMode = CalendarMode.Month;
        MonthCalendar.UpdateLayout();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            MonthCalendar.DisplayMode = CalendarMode.Year;

            _internalDisplayModeChange = false;
        }), DispatcherPriority.Loaded);
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
                        dayWindow.Owner = this;
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
                {
                    taskList.Clear();
                    foreach (var item in loaded)
                        taskList.Add(item);  // 🟢 기존 taskList에 추가
                }
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
            var editWindow = new AddTaskWindow(selectedTask)
            {
                Owner = this  // MainWindow를 오너로 지정
            };

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
        double canvasWidth = ScheduleCanvas.ActualWidth;
        double canvasHeight = ScheduleCanvas.ActualHeight;

        double hourHeight = canvasHeight / 24.0;    // 1시간 = 캔버스 높이÷24
        double columnWidth = canvasWidth / 7.0;      // 1일  = 캔버스 너비÷7

        // 기존에 그린 선 지우기 (필요 시)
        foreach (var line in ScheduleCanvas.Children.OfType<Line>().ToArray())
            ScheduleCanvas.Children.Remove(line);

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
        TimeScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        HeaderScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    // 드래그 시작 (일정 추가용)
    private void ScheduleScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var orig = e.OriginalSource as DependencyObject;
        while (orig != null)
        {
            if (orig is Border b && b.Name == "ScheduleBlock")
                return;
            orig = VisualTreeHelper.GetParent(orig);
        }

        // 캔버스 좌표
        var canvasPos = e.GetPosition(ScheduleCanvas);
        if (canvasPos.X < 0 || canvasPos.Y < 0 ||
            canvasPos.X > ScheduleCanvas.ActualWidth || canvasPos.Y > ScheduleCanvas.ActualHeight)
            return;

        _dragStartPoint = canvasPos;
        _isDragging = true;

        double columnWidth = ScheduleCanvas.ActualWidth / 7.0;

        _newRectPreview = new Rectangle
        {
            Stroke = Brushes.Gray,
            StrokeDashArray = new DoubleCollection { 3, 3 },
            Fill = new SolidColorBrush(Color.FromArgb(50, 135, 206, 250)),
            Width = columnWidth, // 동적 폭
            Height = 0
        };

        int dayIndex = Math.Clamp((int)(canvasPos.X / columnWidth), 0, 6);
        Canvas.SetLeft(_newRectPreview, dayIndex * columnWidth);
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

        const double dragThreshold = 1.0;
        if (_newRectPreview.Height < dragThreshold)
        {
            ScheduleCanvas.Children.Remove(_newRectPreview);
            _newRectPreview = null;
            return;
        }

        try
        {
            var endPoint = e.GetPosition(ScheduleCanvas);
            double canvasHeight = ScheduleCanvas.ActualHeight;
            double columnWidth = ScheduleCanvas.ActualWidth / 7.0;

            int dayIndex = Math.Clamp((int)(_dragStartPoint.X / columnWidth), 0, 6);

            double y1 = Math.Min(_dragStartPoint.Y, endPoint.Y);
            double y2 = Math.Max(_dragStartPoint.Y, endPoint.Y);

            // 픽셀 → 분 단위 환산
            double startMinutes = y1 * 24 * 60 / canvasHeight;
            double endMinutes = y2 * 24 * 60 / canvasHeight;

            var st = TimeSpan.FromMinutes(startMinutes);
            var et = TimeSpan.FromMinutes(endMinutes);

            var dlgVm = new ScheduleDialogViewModel(null, viewModel.ScheduleItems);
            dlgVm.Day = (DayOfWeek)dayIndex;
            dlgVm.StartTime = st;
            dlgVm.EndTime = et;

            var dlg = new ScheduleDialog(dlgVm) { Owner = this };
            bool? result = dlg.ShowDialog();
            if (result == true && dlgVm.NewItem != null)
            {
                var newItem = dlgVm.NewItem;

                viewModel.ScheduleItems.Add(dlgVm.NewItem);
                SaveSchedules();
            }
        }
        finally
        {
            ScheduleCanvas.Children.Remove(_newRectPreview);
            _newRectPreview = null;
        }
    }

    // 블록 클릭 → 편집/삭제 다이얼로그
    private void ScheduleBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_newRectPreview != null)
        {
            ScheduleCanvas.Children.Remove(_newRectPreview);
            _newRectPreview = null;
            _isDragging = false;
        }

        if (!(sender is FrameworkElement fe) || !(fe.DataContext is ScheduleItem item))
            return;

        var dlgVm = new ScheduleDialogViewModel(item, viewModel.ScheduleItems);
        var dlg = new ScheduleDialog(dlgVm) { Owner = this };
        bool? result = dlg.ShowDialog();

        if (result == false && dlgVm.RequestDelete)
        {
            viewModel.ScheduleItems.Remove(item);
            SaveSchedules();
        }
        else if (result == true && !dlgVm.RequestDelete)
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

    private void ScheduleScrollViewer_MouseLeave(object sender, MouseEventArgs e)
    {
        // 드래그 중이었으면 취소하고 미리보기 제거
        if (_isDragging && _newRectPreview != null)
        {
            _isDragging = false;
            ScheduleCanvas.Children.Remove(_newRectPreview);
            _newRectPreview = null;
        }
    }
    #endregion

    #region 메모 관련 코드
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetPlaceholder();
    }

    //private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
        
    //}

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlanMate.ViewModels.MainViewModel.SelectedMemo))
        {
            MainTab.SelectedIndex = 3;
            TitleBox.Focus();
            Placeholder.Visibility = Visibility.Collapsed;
        }
    }

    private void MemoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SetPlaceholder();
    }

    private void ContentBox_GotFocus(object sender, RoutedEventArgs e)
    {
        Placeholder.Visibility = Visibility.Collapsed;
    }

    private void ContentBox_LostFocus(object sender, RoutedEventArgs e)
    {
        SetPlaceholder();
    }

    private void SetPlaceholder()
    {
        Placeholder.Visibility = string.IsNullOrWhiteSpace(ContentBox.Text)
                                  ? Visibility.Visible
                                  : Visibility.Collapsed;

        if (viewModel.SelectedMemo == null)
            Placeholder.Visibility = Visibility.Collapsed;
    }

    private bool isListVisible = true;

    private void ToggleListPanel_Click(object sender, RoutedEventArgs e)
    {
        isListVisible = !isListVisible;

        LeftColumn.Width = isListVisible ? new GridLength(150) : new GridLength(0);
        ToggleButton.Content = isListVisible ? "◀" : "▶";
        ToggleButton.Margin = isListVisible
            ? new Thickness(150, 10, 0, 0)  // 패널 보일 때 오른쪽에
            : new Thickness(0, 10, 0, 0);   // 패널 숨겨질 때 왼쪽으로 이동
    }


    #endregion
}