using PlanMate.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanMate.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
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
            // 샘플 데이터
            ScheduleItems.Add(new ScheduleItem
            {
                Title = "아침 조깅",
                Day = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(6),
                EndTime = TimeSpan.FromHours(7)
            });
            ScheduleItems.Add(new ScheduleItem
            {
                Title = "회의",
                Day = DayOfWeek.Wednesday,
                StartTime = TimeSpan.FromHours(14),
                EndTime = TimeSpan.FromHours(15.5)
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}