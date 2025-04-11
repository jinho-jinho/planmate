using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanMate.Models
{
    public class TaskItem
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Importance { get; set; } // "상", "중", "하"
        public string? Details { get; set; }
        public bool IsCompleted { get; set; }

        public string DDay
        {
            get
            {
                int days = (EndDate.Date - DateTime.Today).Days;
                if (days == 0) return "D-Day";
                if (days > 0) return $"D-{days}";
                return $"D+{-days}";
            }
        }

    }

}