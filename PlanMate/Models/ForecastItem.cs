using System;

namespace PlanMate.Models
{
    public class ForecastItem
    {
        public DateTime DateTime { get; set; }
        public double Temp { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }
}