using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanMate.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public string CurrentDate => DateTime.Now.ToString("yyyy/MM/dd");

        public event PropertyChangedEventHandler PropertyChanged;
    }
}