using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlanMate.Models;
using PlanMate.Services;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace PlanMate.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private const string JsonFileName = "schedules.json";

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

        // 커맨드
        public ICommand AddScheduleCommand { get; }
        public ICommand AddMemoCommand { get; }
        public ICommand RemoveMemoCommand { get; }

        public string CurrentDate => DateTime.Now.ToString("yyyy/MM/dd");

        // 시간축 레이블: 0:00 ~ 23:00
        public ObservableCollection<string> TimeLabels { get; }
            = new ObservableCollection<string>(
                Enumerable.Range(0, 24).Select(i => $"{i:00}:00"));

        // 시간표 아이템 컬렉션 추가
        public ObservableCollection<ScheduleItem> ScheduleItems { get; } 
            = new ObservableCollection<ScheduleItem>();

        public MainViewModel()
        {
            #region 시간표
            // JSON 파일에서 기존 일정 로드(있는 경우)
            LoadFromJson();

            // 컬렉션 변경 시 자동 저장
            ScheduleItems.CollectionChanged += (s, e) => SaveToJson();

            // ＋ 버튼에 바인딩할 커맨드
            AddScheduleCommand = new RelayCommand(
                _ => OnAddSchedule(),
                _ => true
            );
            #endregion

            #region 메모
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
            #endregion
        }

        #region 메모
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

        #region JSON 저장/로드

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

        #endregion

        #region 커맨드 핸들러

        private void OnAddSchedule()
        {
            // ScheduleDialog를 추가 모드로 열기
            var dlgVm = new ScheduleDialogViewModel(timeLabels: TimeLabels);
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
    }
}