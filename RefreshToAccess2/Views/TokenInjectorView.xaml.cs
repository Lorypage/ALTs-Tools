using RefreshToAccess2.ViewModels;
using RefreshToAccess2.Views.Inject;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RefreshToAccess2.Views
{
    public partial class TokenInjectorView : System.Windows.Controls.UserControl
    {
        private MainViewModel RootVM =>
            (MainViewModel)Window.GetWindow(this)!.DataContext;

        public TokenInjectorView()
        {
            InitializeComponent();
        }

        // ── Open process selector ──────────────────────────────────────

        private async void OnOpenProcSelector(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.InjectInitSuccess)
            {
                ShowStatus(
                    "Injector failed to initialise – " +
                    "ensure only one instance is running.");
                return;
            }

            var selector = new MinecraftProcSelectorView();
            await MaterialDesignThemes.Wpf.DialogHost.Show(
                selector, Helpers.AppMessageBox.RootDialogIdentifier);
        }

        // ── Status label fade-in ───────────────────────────────────────

        private void ShowStatus(string message)
        {
            InjectorStatus.Text    = message;
            InjectorStatus.Opacity = 0;

            InjectorStatus.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromSeconds(0.3)));
        }
    }
}
