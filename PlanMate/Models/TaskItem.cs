using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PlanMate.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public string Name { get; set; }

        private DateTime startDate;
        public DateTime StartDate
        {
            get => startDate;
            set
            {
                if (startDate != value)
                {
                    startDate = value;
                    Notify(nameof(StartDate));
                    Notify(nameof(StartDDay));
                }
            }
        }

        private DateTime endDate;
        public DateTime EndDate
        {
            get => endDate;
            set
            {
                if (endDate != value)
                {
                    endDate = value;
                    Notify(nameof(EndDate));
                    Notify(nameof(EndDDay));
                    Notify(nameof(DDay));
                }
            }
        }

        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Importance { get; set; } // "상", "중", "하"
        public string? Details { get; set; }
        public bool IsCompleted { get; set; }
        public List<string> RelatedDocs { get; set; } = new();

        public string DDay => CalcDDay(EndDate);
        public string StartDDay => CalcDDay(StartDate);
        public string EndDDay => CalcDDay(EndDate);

        private string CalcDDay(DateTime date)
        {
            int days = (date.Date - DateTime.Today).Days;
            return days switch
            {
                0 => "D-Day",
                > 0 => $"D-{days}",
                _ => $"D+{-days}"
            };
        }
    }
}
