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
    private void SortByImportanceThenDDay_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;

        var sorted = taskList.OrderBy(t =>
            t.Importance == "상" ? 0 :
            t.Importance == "중" ? 1 : 2
        ).ThenBy(t => (t.EndDate - today).Days).ToList();

        taskList.Clear();
        foreach (var t in sorted)
            taskList.Add(t);
    }

    private void SortByDDayThenImportance_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;

        var sorted = taskList.OrderBy(t => (t.EndDate - today).Days)
            .ThenBy(t =>
                t.Importance == "상" ? 0 :
                t.Importance == "중" ? 1 : 2
            ).ToList();

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
