using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanMate.Models
{
    public class ChatMessage
    {
        public string Role { get; set; } // "User" 또는 "Bot"
        public string Message { get; set; }
    }

}
