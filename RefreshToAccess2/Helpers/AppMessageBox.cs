using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Views.Dialogs;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Drop-in replacement for <see cref="System.Windows.MessageBox"/> that renders
    /// the prompt inside the application window (via the root <see cref="DialogHost"/>)
    /// instead of spawning a separate OS-level window.
    ///
    /// The public <c>Show</c> overloads mirror the signatures used across the codebase
    /// and remain synchronous/blocking, returning a <see cref="MessageBoxResult"/>.
    /// If no <see cref="DialogHost"/> is available yet (e.g. during startup or a crash
    /// before the main window exists) it falls back to the native message box.
    /// </summary>
    public static class AppMessageBox
    {
        /// <summary>Identifier of the root DialogHost declared in MainWindow.xaml.</summary>
        public const string RootDialogIdentifier = "RootDialog";

        // Stack of active DialogHost identifiers. The root host sits at the bottom;
        // in-app dialogs that themselves raise message boxes push their own inner
        // host so nested prompts render on top instead of failing/falling back.
        private static readonly System.Collections.Generic.Stack<string> _hostStack
            = new();

        /// <summary>Registers an inner DialogHost as the current target for prompts.</summary>
        public static void PushHost(string identifier) => _hostStack.Push(identifier);

        /// <summary>Removes the most recently pushed host (no-op if it isn't on top).</summary>
        public static void PopHost(string identifier)
        {
            if (_hostStack.Count > 0 && _hostStack.Peek() == identifier)
                _hostStack.Pop();
        }

        private static string CurrentHost
            => _hostStack.Count > 0 ? _hostStack.Peek() : RootDialogIdentifier;

        public static MessageBoxResult Show(string messageBoxText)
            => Show(messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption)
            => Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(
            string messageBoxText, string caption, MessageBoxButton button)
            => Show(messageBoxText, caption, button, MessageBoxImage.None);

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            Application? app = Application.Current;

            // No application/dispatcher at all — last resort native box.
            if (app?.Dispatcher is null)
                return System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

            // Marshal onto the UI thread; block the caller until it returns.
            if (!app.Dispatcher.CheckAccess())
            {
                return app.Dispatcher.Invoke(
                    () => ShowOnUiThread(messageBoxText, caption, button, icon));
            }

            return ShowOnUiThread(messageBoxText, caption, button, icon);
        }

        private static MessageBoxResult ShowOnUiThread(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            // Fall back to the native box when the host isn't mounted/open yet.
            string host = CurrentHost;
            if (!DialogHostIsAvailable(host))
                return System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

            var dialog = new MessageDialog(messageBoxText, caption, button, icon);

            // DialogHost.Show is async; pump a nested dispatcher frame so the
            // call blocks synchronously the way MessageBox.Show does.
            var frame = new DispatcherFrame();
            MessageBoxResult result = MessageBoxResult.None;

            async void Run()
            {
                try
                {
                    await DialogHost.Show(dialog, host);
                    result = dialog.Result;
                }
                catch
                {
                    // If the host rejects the call (e.g. already open), degrade
                    // gracefully to the native box.
                    result = System.Windows.MessageBox.Show(
                        messageBoxText, caption, button, icon);
                }
                finally
                {
                    frame.Continue = false;
                }
            }

            Run();
            Dispatcher.PushFrame(frame);
            return result;
        }

        private static bool DialogHostIsAvailable(string identifier)
        {
            // DialogHost.IsDialogOpen throws when no host with this identifier is
            // loaded (e.g. before the main window exists). A registered host that
            // is merely closed returns false. We require a registered, loaded host.
            try
            {
                _ = DialogHost.IsDialogOpen(identifier);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
