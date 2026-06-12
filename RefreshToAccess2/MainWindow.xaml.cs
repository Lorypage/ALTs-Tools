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
using System.Windows.Input;
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
        private int _currentPageIndex = -1;

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
                InjectorView,
                SkinChangerView,
                SettingsView,
                };

                ShowPage(0);
                NavListBox.SelectedIndex = 0;

                _ = InitialiseAsync();
            }
            catch (Exception ex) { 
                Helper.PopException(ex);
            }
        }

        private void NavListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged)
                return;

            if (NavListBox.SelectedIndex < 0)
                return;

            int index = NavListBox.SelectedIndex;

            ViewModel.SelectedNavIndex = index;
            ShowPage(index);
        }

        private void ShowPage(int index)
        {
            int previous = _currentPageIndex;
            _currentPageIndex = index;

            foreach (UIElement page in _pages)
            {
                page.Visibility = Visibility.Collapsed;
            }

            if (index >= 0 && index < _pages.Length)
            {
                UIElement targetPage = _pages[index];

                // Bump entrance animation generation BEFORE making visible.
                // This ensures all children's IsVisibleChanged handlers
                // see the new generation and re-animate.
                Helpers.EntranceAnimation.BumpGeneration();

                // Slide direction follows nav travel: moving down the rail
                // slides the page up from below, moving up slides from above.
                double fromY = previous >= 0 && index < previous ? -28 : 28;

                targetPage.Opacity = 0;
                targetPage.Visibility = Visibility.Visible;

                if (targetPage.RenderTransform is System.Windows.Media.TranslateTransform transform)
                {
                    transform.Y = fromY;
                }

                AnimatePageIn(targetPage, fromY);
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

        private void AnimatePageIn(UIElement element, double fromY)
        {
            // Eased glide for the slide, soft fade for the opacity.
            var slideEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            var fadeEase = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            TimeSpan duration = TimeSpan.FromMilliseconds(340);

            Storyboard sb = new();

            DoubleAnimation fadeAnim = new()
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = fadeEase
            };
            Storyboard.SetTarget(fadeAnim, element);
            Storyboard.SetTargetProperty(fadeAnim, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeAnim);

            DoubleAnimation floatAnim = new()
            {
                From = fromY,
                To = 0,
                Duration = duration,
                EasingFunction = slideEase
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
                        Localization.Loc.T("Main.Msg.ListenerFailed"),
                        Localization.Loc.T("Main.Msg.ListenerFailedTitle"),
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
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            Helpers.TitleBarHelper.Apply(this);

            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);

            // Force-hide keyboard cue indicators (focus rects + mnemonic underlines)
            // WM_CHANGEUISTATE = 0x0127, UIS_SET = 1, UISF_HIDEFOCUS = 1, UISF_HIDEACCEL = 2
            SendMessage(hwnd, 0x0127, (IntPtr)((0x0003 << 16) | 1), IntPtr.Zero);

            // Intercept future attempts to show them
            source?.AddHook(WndProc);
        }

        private static IntPtr WndProc(
            IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // WM_UPDATEUISTATE = 0x0128
            // Block UIS_CLEAR (action=2) which shows keyboard indicators
            // Allow UIS_SET (action=1) which hides them
            if (msg == 0x0128)
            {
                int action = wParam.ToInt32() & 0xFFFF;
                if (action == 2) // UIS_CLEAR — trying to show indicators
                    handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Suppress bare Alt from activating menu navigation mode.
            // Alt+F4, Alt+Tab etc. still work — they arrive as SystemKey=F4/Tab,
            // not SystemKey=LeftAlt/RightAlt.
            if (e.Key == Key.System &&
                (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
            {
                e.Handled = true;
            }
        }
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
