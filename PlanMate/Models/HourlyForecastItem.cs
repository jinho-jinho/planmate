using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanMate.Models
{
    public class HourlyForecastItem
    {
        public DateTime DateTime { get; set; }
        public double Temp { get; set; }
        public string Icon { get; set; }
    }
}
