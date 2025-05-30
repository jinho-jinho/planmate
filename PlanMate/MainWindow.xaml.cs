using PlanMate.Models;
using PlanMate.ViewModels;
using PlanMate.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
    private readonly string savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "tasks.json");
    string memoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlanMate", "memo.json");
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

        // 윈도우 로드 완료 시점에 DrawLines() 메서드를 자동으로 실행
        Loaded += (s, e) => DrawLines();
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var addWindow = new AddTaskWindow();
        if (addWindow.ShowDialog() == true)
        {
            taskList.Add(addWindow.CreatedTask);
            RefreshTaskList();
            SaveTasks();
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
    private void RefreshTaskList()
    {
        var today = DateTime.Today;

        var sorted = taskList.OrderBy(t =>
            t.Importance == "상" ? 0 :
            t.Importance == "중" ? 1 : 2
        ).ThenBy(t => Math.Abs((t.EndDate - today).Days)) // 오늘 기준 종료일이 가까울수록
        .ToList();

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
}
