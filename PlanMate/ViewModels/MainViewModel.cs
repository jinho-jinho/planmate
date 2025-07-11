﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlanMate.Models;
using System.Windows.Media;
using PlanMate.Services;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using PlanMate.Views;
using System.Diagnostics;
using System.Windows.Threading;
using PlanMate.ViewModels;
using PlanMate;
using System.Linq; // .First()와 같은 LINQ 메서드 사용을 위해 추가


namespace PlanMate.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- 속성 및 필드 선언 ---

        public ObservableCollection<TaskItem> TaskList { get; } = new();
        public ICommand DeleteTaskCommand { get; }

        // JsonFileName은 한 번만 선언합니다.
        private readonly string JsonFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PlanMate",
            "schedules.json");

        // AddScheduleCommand는 한 번만 선언합니다.
        public ICommand AddScheduleCommand { get; }
        public ICommand AddMemoCommand { get; }
        public ICommand RemoveMemoCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string CurrentDate => DateTime.Now.ToString("yyyy/MM/dd");

        #region 메모
        private const string MemoJsonFileName = "memos.json";
        private readonly JsonStorageService _memoStorageService;

        public ObservableCollection<MemoItem> Memos { get; } = new ObservableCollection<MemoItem>();

        private MemoItem _selectedMemo;
        public MemoItem SelectedMemo
        {
            get => _selectedMemo;
            set
            {
                if (_selectedMemo != null)
                    _selectedMemo.PropertyChanged -= MemoPropertyChanged;
                _selectedMemo = value;
                OnPropertyChanged(nameof(SelectedMemo));
                if (_selectedMemo != null)
                    _selectedMemo.PropertyChanged += MemoPropertyChanged;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        #endregion

        #region 시간표
        // 시간축 레이블: 0:00 ~ 23:00
        public ObservableCollection<string> TimeLabels { get; }
            = new ObservableCollection<string>(
                Enumerable.Range(0, 24).Select(i => $"{i:00}:00"));

        // 시간표 아이템 컬렉션 추가
        public ObservableCollection<ScheduleItem> ScheduleItems { get; }
            = new ObservableCollection<ScheduleItem>();
        #endregion

        #region 날씨
        private string _location;
        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        private string _temperature;
        public string Temperature
        {
            get => _temperature;
            set { _temperature = value; OnPropertyChanged(nameof(Temperature)); }
        }

        private BitmapImage _weatherIcon;
        public BitmapImage WeatherIcon
        {
            get => _weatherIcon;
            set { _weatherIcon = value; OnPropertyChanged(nameof(WeatherIcon)); }
        }

        private string _weatherDescription;
        public string WeatherDescription
        {
            get => _weatherDescription;
            set { _weatherDescription = value; OnPropertyChanged(nameof(WeatherDescription)); }
        }

        public ObservableCollection<HourlyForecastItem> HourlyForecasts { get; }
            = new ObservableCollection<HourlyForecastItem>();

        public ICommand ShowForecastCommand { get; }

        private readonly WeatherService _weatherService;
        private readonly DispatcherTimer _timer;
        #endregion

        #region 배경색 관련 속성 및 메서드

        private Brush _mainBackground = Brushes.LightGray;
        public Brush MainBackground
        {
            get => _mainBackground;
            set
            {
                _mainBackground = value;
                OnPropertyChanged(nameof(MainBackground));
            }
        }

        public void ChangeBackground(string color)
        {
            try
            {
                MainBackground = (Brush)new BrushConverter().ConvertFromString(color);
            }
            catch
            {
                MessageBox.Show($"'{color}'는 유효한 색상명이 아닙니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        // --- 생성자 ---
        public MainViewModel()
        {
            // --- 시간표 관련 초기화 ---
            LoadFromJson();
            DeleteTaskCommand = new RelayCommand(DeleteTask, obj => obj is TaskItem);
            ScheduleItems.CollectionChanged += (s, e) => SaveToJson();
            AddScheduleCommand = new RelayCommand(
                _ => OnAddSchedule(),
                _ => true
            );

            // --- 메모 관련 초기화 ---
            _memoStorageService = new JsonStorageService(MemoJsonFileName);
            var saved = _memoStorageService.LoadMemos();
            foreach (var memo in saved)
            {
                Memos.Add(memo);
                memo.PropertyChanged += MemoPropertyChanged;
            }
            if (Memos.Any()) SelectedMemo = Memos.First();
            Memos.CollectionChanged += MemoCollectionChanged;

            AddMemoCommand = new RelayCommand(_ => AddMemo());
            RemoveMemoCommand = new RelayCommand(_ => RemoveMemo(), _ => SelectedMemo != null);

            // --- 날씨 관련 초기화 ---
            _weatherService = new WeatherService();
            ShowForecastCommand = new RelayCommand(
                _ => LoadHourlyForecastAsync(),
                _ => true
            );

            LoadWeatherAsync();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
            _timer.Tick += async (_, __) => await LoadWeatherAsync();
            _timer.Start();
        }

        // --- 메서드 ---

        private void DeleteTask(object obj)
        {
            if (obj is TaskItem task)
            {
                if (MessageBox.Show($"{task.Name} 일정을 삭제하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    TaskList.Remove(task);
                    // 예시: TaskList 저장 로직 필요시 추가
                }
            }
        }

        #region 시간표 기능
        private void LoadFromJson()
        {
            try
            {
                if (!File.Exists(JsonFileName))
                    return;

                string json = File.ReadAllText(JsonFileName).Trim();
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var list = JsonSerializer.Deserialize<ObservableCollection<ScheduleItem>>(json, options);
                if (list != null && list.Count > 0)
                {
                    ScheduleItems.Clear();
                    foreach (var item in list)
                        ScheduleItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"일정 로딩 중 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveToJson()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(ScheduleItems, options);
                File.WriteAllText(JsonFileName, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"일정 저장 중 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAddSchedule()
        {
            // ScheduleDialog를 추가 모드로 열기
            var dlgVm = new ScheduleDialogViewModel(null, ScheduleItems, timeLabels: TimeLabels);
            var dlg = new PlanMate.Views.ScheduleDialog(dlgVm)
            {
                Owner = Application.Current.MainWindow
            };
            var result = dlg.ShowDialog();
            if (result == true && dlgVm.NewItem != null)
            {
                ScheduleItems.Add(dlgVm.NewItem);
            }
        }
        #endregion

        #region 메모 기능
        private void MemoCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (MemoItem m in e.NewItems) m.PropertyChanged += MemoPropertyChanged;
            if (e.OldItems != null) foreach (MemoItem m in e.OldItems) m.PropertyChanged -= MemoPropertyChanged;
            SaveMemos();
        }

        private void MemoPropertyChanged(object sender, PropertyChangedEventArgs e) => SaveMemos();

        private void AddMemo()
        {
            var memo = new MemoItem { Title = "새 메모", Content = string.Empty };
            Memos.Add(memo);
            SelectedMemo = memo;
        }

        private void RemoveMemo()
        {
            if (SelectedMemo == null) return;
            var toRemove = SelectedMemo;
            SelectedMemo = Memos.FirstOrDefault(m => m != toRemove);
            Memos.Remove(toRemove);
        }

        private void SaveMemos() => _memoStorageService.SaveMemos(Memos);
        #endregion

        #region 날씨 기능
        private async Task LoadWeatherAsync()
        {
            try
            {
                var (city, temp, icon) = await _weatherService.GetCurrentWeatherAsync();
                Location = city;
                Temperature = $"{temp:F1}°C";
                WeatherIcon = new BitmapImage(new Uri($"https://openweathermap.org/img/wn/{icon}@2x.png"));
                OnPropertyChanged(nameof(CurrentDate));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Weather load failed: {ex.Message}");
            }
        }

        private async Task LoadHourlyForecastAsync()
        {
            try
            {
                var list = await _weatherService.Get5DayForecastAsync();
                HourlyForecasts.Clear();
                // 예: 당일 오전 기준 24시간치만
                foreach (var item in list.Take(24))
                {
                    HourlyForecasts.Add(new HourlyForecastItem
                    {
                        DateTime = item.DateTime,
                        Temp = item.Temp,
                        Icon = item.Icon
                    });
                }
                WeatherDescription = list.FirstOrDefault()?.Description;

                var window = new PlanMate.Views.ForecastWindow { DataContext = this };

                window.Owner = App.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = window.Owner.Left + 20;
                window.Top = window.Owner.Top + 50;

                window.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Forecast load failed: {ex.Message}");
            }
        }
        #endregion
    }
}