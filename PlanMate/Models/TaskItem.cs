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
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Importance { get; set; } // "상", "중", "하"
        public string? Details { get; set; }
        public bool IsCompleted { get; set; }
    }
}