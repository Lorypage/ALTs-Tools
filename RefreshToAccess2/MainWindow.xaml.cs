using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RefreshToAccess2
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static bool InjectInitSuccess { get; private set; }

        private static readonly HttpListener _listener = new();
        public System.Windows.Controls.ListBox NavListBoxControl => NavListBox;

        public MainViewModel ViewModel { get; } = new MainViewModel();

        private bool _railExpanded = false;
        private const double RailCollapsed = 80;
        private const double RailExpanded = 90;
        private const double AnimMs = 200;

        private UIElement[] _pages = Array.Empty<UIElement>();
        private bool _suppressSelectionChanged;

        public bool IsRailExpanded
        {
            get => _railExpanded;
            private set
            {
                if (_railExpanded == value) return;
                _railExpanded = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            try
            {
                DirectoryInfo dogshit = new DirectoryInfo(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Temp\\.net"));
                dogshit.Delete(true);
            }
            catch { }

            DataContext = ViewModel;
            InitializeComponent();

            _pages = new UIElement[]
            {
                ConverterView,
                AltManagerView,
                IGNRenameView,
                InjectorView,
                SkinChangerView,
            };

            ShowPage(0);
            NavListBox.SelectedIndex = 0;

            _ = InitialiseAsync();
        }

        private void NavListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged)
                return;

            if (NavListBox.SelectedIndex < 0)
                return;

            int index = NavListBox.SelectedIndex;

            // Only Renamer requires login.
            if (index == 2 && !ViewModel.Converter.LoggedIn)
            {
                MessageBox.Show(
                    "Please convert a refresh token first before using this feature.",
                    "Not logged in",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                try
                {
                    _suppressSelectionChanged = true;
                    NavListBox.SelectedIndex = ViewModel.SelectedNavIndex;
                }
                finally
                {
                    _suppressSelectionChanged = false;
                }

                return;
            }

            ViewModel.SelectedNavIndex = index;
            ShowPage(index);
        }

        private void ShowPage(int index)
        {
            foreach (UIElement page in _pages)
            {
                page.Visibility = Visibility.Collapsed;
            }

            if (index >= 0 && index < _pages.Length)
            {
                UIElement targetPage = _pages[index];

                targetPage.Opacity = 0;
                targetPage.Visibility = Visibility.Visible;

                if (targetPage.RenderTransform is System.Windows.Media.TranslateTransform transform)
                {
                    transform.Y = 20;
                }

                AnimatePageIn(targetPage);
            }
        }

        private void OnToggleRail(object sender, RoutedEventArgs e)
        {
            IsRailExpanded = !IsRailExpanded;
            AnimateRail(IsRailExpanded);
        }

        private void AnimateRail(bool expand)
        {
            double targetWidth = expand ? RailExpanded : RailCollapsed;

            var widthAnim = new Helpers.GridLengthAnimation
            {
                From = NavColumn.Width,
                To = new GridLength(targetWidth),
                Duration = TimeSpan.FromMilliseconds(AnimMs),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };

            NavColumn.BeginAnimation(ColumnDefinition.WidthProperty, widthAnim);

            ToggleIcon.Kind = expand
                ? MaterialDesignThemes.Wpf.PackIconKind.MenuOpen
                : MaterialDesignThemes.Wpf.PackIconKind.Menu;
        }

        private void AnimatePageIn(UIElement element)
        {
            QuadraticEase ease = new() { EasingMode = EasingMode.EaseOut };
            TimeSpan duration = TimeSpan.FromMilliseconds(300);

            Storyboard sb = new();

            DoubleAnimation fadeAnim = new()
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(fadeAnim, element);
            Storyboard.SetTargetProperty(fadeAnim, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeAnim);

            DoubleAnimation floatAnim = new()
            {
                From = 20,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(floatAnim, element);
            Storyboard.SetTargetProperty(floatAnim,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            sb.Children.Add(floatAnim);

            sb.Begin();
        }

        private async Task InitialiseAsync()
        {
            await Task.Run(() =>
            {
                try { Helper.ExtractInjectionDll(); }
                catch (Exception ex) { Helper.PopException(ex); }
            });

            await StartListenerAsync();
        }

        private async Task StartListenerAsync()
        {
            try
            {
                _listener.Prefixes.Add("http://localhost:38964/");
                _listener.Start();
                InjectInitSuccess = true;

                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() =>
                        TokenInjectionService.HandleRequestAsync(context));
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        "Failed to initialise the token injection listener.\n" +
                        "Make sure only one instance of this program is running.",
                        "Injection init failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning));
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            try { _listener.Stop(); } catch { }
            try
            {
                if (!string.IsNullOrEmpty(Helper.tmpFileName))
                    File.Delete(Helper.tmpFileName);
            }
            catch { }

            try { } catch { }
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            Helpers.TitleBarHelper.Apply(this);
        }
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
