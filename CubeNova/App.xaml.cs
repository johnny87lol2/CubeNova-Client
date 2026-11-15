using System.Windows;

namespace CubeNova;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            try { File.AppendAllText(Path.Combine(Paths.Root, "crash.log"), DateTime.Now + "\n" + e.Exception + "\n\n"); } catch { }
            MessageBox.Show(e.Exception.Message, "CubeNova", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
