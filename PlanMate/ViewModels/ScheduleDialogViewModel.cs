using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PlanMate.Models;

namespace PlanMate.ViewModels
{
    public class ScheduleDialogViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DayOfWeek> DaysOfWeek { get; }
            = new ObservableCollection<DayOfWeek>(
                  Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>());

        public ObservableCollection<string> TimeLabels { get; }

        private DayOfWeek _day;
        public DayOfWeek Day
        {
            get => _day;
            set { _day = value; OnPropertyChanged(nameof(Day)); }
        }

        private string _startTimeString;
        public string StartTimeString
        {
            get => _startTimeString;
            set { _startTimeString = value; OnPropertyChanged(nameof(StartTimeString)); }
        }

        private string _endTimeString;
        public string EndTimeString
        {
            get => _endTimeString;
            set { _endTimeString = value; OnPropertyChanged(nameof(EndTimeString)); }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private readonly ScheduleItem _editingItem;
        public bool IsEditMode { get; }

        public ScheduleItem NewItem { get; private set; }

        private bool _requestClose;
        public bool RequestClose
        {
            get => _requestClose;
            private set
            {
                if (_requestClose != value)
                {
                    _requestClose = value;
                    OnPropertyChanged(nameof(RequestClose));
                }
            }
        }

        private bool _requestDelete;
        public bool RequestDelete
        {
            get => _requestDelete;
            private set
            {
                if (_requestDelete != value)
                {
                    _requestDelete = value;
                    OnPropertyChanged(nameof(RequestDelete));
                }
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand DeleteCommand { get; }

        public ScheduleDialogViewModel(
            ScheduleItem existingItem = null,
            ObservableCollection<string> timeLabels = null)
        {
            if (timeLabels != null)
            {
                TimeLabels = timeLabels;
            }
            else
            {
                TimeLabels = new ObservableCollection<string>();
                for (int h = 0; h < 24; h++)
                    TimeLabels.Add($"{h:00}:00");
            }

            if (existingItem != null)
            {
                IsEditMode = true;
                _editingItem = existingItem;

                Day = existingItem.Day;
                StartTimeString = existingItem.StartTime.ToString(@"hh\:mm");
                EndTimeString = existingItem.EndTime.ToString(@"hh\:mm");
                Title = existingItem.Title;
            }
            else
            {
                IsEditMode = false;
                Day = DayOfWeek.Monday;
                StartTimeString = TimeLabels.First();
                EndTimeString = TimeLabels.Skip(1).FirstOrDefault() ?? TimeLabels.First();
                Title = "";
            }

            ConfirmCommand = new RelayCommand(
                _ => OnConfirm(),
                _ => true
            );
            DeleteCommand = new RelayCommand(
                _ => OnDelete(),
                _ => IsEditMode
            );
        }

        private void OnConfirm()
        {
            if (!TimeSpan.TryParse(StartTimeString, out var st) ||
                !TimeSpan.TryParse(EndTimeString, out var et) ||
                string.IsNullOrWhiteSpace(Title) ||
                et <= st)
            {
                MessageBox.Show("시간이나 제목을 올바르게 입력하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsEditMode)
            {
                _editingItem.Day = Day;
                _editingItem.StartTime = st;
                _editingItem.EndTime = et;
                _editingItem.Title = Title;
            }
            else
            {
                NewItem = new ScheduleItem
                {
                    Day = Day,
                    StartTime = st,
                    EndTime = et,
                    Title = Title
                };
            }

            RequestClose = true;
        }

        private void OnDelete()
        {
            RequestDelete = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
