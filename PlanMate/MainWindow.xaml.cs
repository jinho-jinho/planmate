﻿using PlanMate.Models;
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
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PlanMate", "tasks.json");

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        LoadTasks();
        DailyTaskList.ItemsSource = taskList;
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

}
