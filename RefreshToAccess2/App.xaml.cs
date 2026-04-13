using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RefreshToAccess2
{
    public partial class App : Application
    {
        private static readonly string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RefreshToAccess2", "crash.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            // Global exception handlers — prevent silent crashes
            DispatcherUnhandledException += OnDispatcherUnhandled;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandled;
            TaskScheduler.UnobservedTaskException += OnTaskUnobserved;

            base.OnStartup(e);
        }

        private void OnDispatcherUnhandled(
            object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandled", e.Exception);
            ShowCrashDialog(e.Exception);
            e.Handled = true;
        }

        private void OnDomainUnhandled(
            object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogCrash("AppDomainUnhandled", ex);
        }

        private void OnTaskUnobserved(
            object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTask", e.Exception);
            e.SetObserved(); // Prevent process termination
        }

        private static void LogCrash(string source, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n" +
                               $"{ex}\n" +
                               $"{"".PadRight(80, '=')}\n";
                File.AppendAllText(_logPath, entry);
            }
            catch { /* last resort — can't even log */ }
        }

        private static void ShowCrashDialog(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n" +
                    $"{ex.Message}\n\n" +
                    $"Details have been written to:\n{_logPath}",
                    "Token Tools — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch { }
        }
    }
}
