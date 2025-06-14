using PlanMate.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PlanMate.ViewModels
{
    public class ScheduleDialogViewModel : INotifyPropertyChanged
    {
        private readonly IEnumerable<ScheduleItem> _allItems;

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

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public DateTime StartDateTime
        {
            get => DateTime.Today + StartTime;
            set
            {
                StartTime = value.TimeOfDay;
                OnPropertyChanged(nameof(StartDateTime));
                OnPropertyChanged(nameof(StartTime));
            }
        }

        public DateTime EndDateTime
        {
            get => DateTime.Today + EndTime;
            set
            {
                EndTime = value.TimeOfDay;
                OnPropertyChanged(nameof(EndDateTime));
                OnPropertyChanged(nameof(EndTime));
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private Color _Color = Colors.White;
        public Color Color
        {
            get => _Color;
            set
            {
                _Color = value;
                OnPropertyChanged(nameof(Color));
            }
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
            ScheduleItem existingItem,
            IEnumerable<ScheduleItem> allItems,
            ObservableCollection<string> timeLabels = null)
        {
            _allItems = allItems;

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
                StartTime = existingItem.StartTime;
                EndTime = existingItem.EndTime;
                Title = existingItem.Title;
                Color = existingItem.Color;
            }
            else
            {
                IsEditMode = false;
                Day = DayOfWeek.Monday;
                StartTime = TimeSpan.FromHours(9);
                EndTime = TimeSpan.FromHours(10);
                Title = "";
                Color = Colors.LightBlue;
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

        bool IsOverlapping(TimeSpan s1, TimeSpan e1, TimeSpan s2, TimeSpan e2)
            => s1 < e2 && e1 > s2;

        private void OnConfirm()
        {
            if (string.IsNullOrWhiteSpace(Title) || EndTime <= StartTime)
            {
                MessageBox.Show("시간이나 제목을 올바르게 입력하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var conflict = _allItems
                .Where(x => x != _editingItem && x.Day == Day)
                .Any(x => IsOverlapping(x.StartTime, x.EndTime,
                StartTime, EndTime));

            if (conflict)
            {
                MessageBox.Show("다른 일정과 겹칩니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsEditMode)
            {
                _editingItem.Day = Day;
                _editingItem.StartTime = StartTime;
                _editingItem.EndTime = EndTime;
                _editingItem.Title = Title;
                _editingItem.Color = Color;
            }
            else
            {
                NewItem = new ScheduleItem
                {
                    Day = Day,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    Title = Title,
                    Color = Color
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
