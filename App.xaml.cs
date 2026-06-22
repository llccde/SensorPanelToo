using System.Windows;
using System.Windows.Threading;

namespace SensorPanelToo;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"Unhandled error: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
