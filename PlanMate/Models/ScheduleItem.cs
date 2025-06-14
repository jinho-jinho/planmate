using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace PlanMate.Models
{
    public class ScheduleItem : INotifyPropertyChanged
    {
        private DayOfWeek _day;
        public DayOfWeek Day
        {
            get => _day;
            set
            {
                if (_day != value)
                {
                    _day = value;
                    OnPropertyChanged(nameof(Day));
                    OnPropertyChanged(nameof(Left));
                }
            }
        }

        private TimeSpan _startTime;
        public TimeSpan StartTime   
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                    OnPropertyChanged(nameof(Top));
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        private TimeSpan _endTime;
        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged(nameof(EndTime));
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        private Color _color = Colors.LightBlue;
        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
                    OnPropertyChanged(nameof(Background));
                }
            }
        }

        [JsonIgnore]
        public SolidColorBrush Background => new SolidColorBrush(Color);

        [JsonIgnore]
        public double Top => StartTime.TotalMinutes;

        [JsonIgnore]
        public double Left => (int)Day * 50.0;

        [JsonIgnore]
        public double Height => (EndTime - StartTime).TotalMinutes;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
