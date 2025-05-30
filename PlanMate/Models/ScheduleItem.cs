using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanMate.Models
{
    public class ScheduleItem
    {
        public string Title { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // 화면 위치 계산 (1분 = 1px)
        public double Top => StartTime.TotalMinutes;
        public double Height => (EndTime - StartTime).TotalMinutes;
    }
}
