using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlanMate.Models;

namespace PlanMate.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string JsonFileName = "schedules.json";

        // 커맨드
        public ICommand AddScheduleCommand { get; }

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
            // JSON 파일에서 기존 일정 로드(있는 경우)
            LoadFromJson();

            // 컬렉션 변경 시 자동 저장
            ScheduleItems.CollectionChanged += (s, e) => SaveToJson();

            // ＋ 버튼에 바인딩할 커맨드
            AddScheduleCommand = new RelayCommand(
                _ => OnAddSchedule(),
                _ => true
            );
        }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}