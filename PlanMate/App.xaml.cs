using System.Configuration;
using System.Data;
using System.Windows;

namespace PlanMate;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        this.DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show("예외 발생: " + args.Exception.Message, "에러");
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}
