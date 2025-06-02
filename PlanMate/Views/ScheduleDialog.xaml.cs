using System.Windows;
using PlanMate.ViewModels;

namespace PlanMate.Views
{
    public partial class ScheduleDialog : Window
    {
        public ScheduleDialog(ScheduleDialogViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.RequestClose) && vm.RequestClose)
                {
                    DialogResult = true;
                }
                else if (e.PropertyName == nameof(vm.RequestDelete) && vm.RequestDelete)
                {
                    DialogResult = false;
                }
            };
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}