using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace RefreshToAccess2.Views
{
    public partial class AltManagerView : UserControl
    {
        private AltManagerViewModel? VM => DataContext as AltManagerViewModel;
        private MainViewModel? RootVM => Window.GetWindow(this)?.DataContext as MainViewModel;

        private readonly SnackbarMessageQueue _snack = new(TimeSpan.FromSeconds(2));
        private bool _barShown, _delShown;
        private BlurEffect? _blur;
        private BlurEffect? _settingsBlur;

        private static readonly CubicEase _easeOut = new() { EasingMode = EasingMode.EaseOut };
        private const double BlurRadius = 24;

        public AltManagerView()
        {
            InitializeComponent();
            Snackbar.MessageQueue = _snack;
            DataContextChanged += OnDCChanged;
        }

        // ── ViewModel wiring ───────────────────────────────────────

        private void OnDCChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AltManagerViewModel old) old.PropertyChanged -= OnVMProp;
            if (e.NewValue is AltManagerViewModel vm)  vm.PropertyChanged  += OnVMProp;
        }

        private void OnVMProp(object? s, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AltManagerViewModel.IsSelectionMode):
                    AnimateBar(VM!.IsSelectionMode); break;
                case nameof(AltManagerViewModel.HasSelection):
                    AnimateDelBtn(VM!.HasSelection); break;
                case nameof(AltManagerViewModel.IsSearching):
                    AnimateSearchBar(VM!.IsSearching); break;
            }
        }

        // ══════════════════════════════════════════════════════════
        //   SEARCH PROGRESS
        // ══════════════════════════════════════════════════════════

        private void AnimateSearchBar(bool show)
        {
            if (show)
            {
                SearchProgress.Visibility = Visibility.Visible;
                SearchProgress.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                    { EasingFunction = _easeOut });
            }
            else
            {
                var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350))
                { EasingFunction = _easeOut };
                fade.Completed += (_, __) =>
                {
                    SearchProgress.BeginAnimation(OpacityProperty, null);
                    SearchProgress.Opacity = 0;
                    if (VM is null || !VM.IsSearching)
                        SearchProgress.Visibility = Visibility.Collapsed;
                };
                SearchProgress.BeginAnimation(OpacityProperty, fade);
            }
        }

        // ══════════════════════════════════════════════════════════
        //   CARD / LIST CLICK
        // ══════════════════════════════════════════════════════════

        private void OnCardClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe ||
                fe.DataContext is not ProfileCardItem item) return;
            if (VM is null) return;

            if (VM.IsSelectionMode) item.IsSelected = !item.IsSelected;
            else OpenDetail(item);
        }

        // ══════════════════════════════════════════════════════════
        //   DETAIL OVERLAY
        // ══════════════════════════════════════════════════════════

        private void OpenDetail(ProfileCardItem item)
        {
            if (VM is null) return;
            VM.DetailItem = item;
            VM.IsDetailOpen = true;

            ClearDetailAnims();
            DetailCard.Opacity = 0;
            CardScale.ScaleX = 0.92; CardScale.ScaleY = 0.92;
            DimBg.Opacity = 0;

            _blur = new BlurEffect { Radius = 0, RenderingBias = RenderingBias.Performance };
            MainContentGrid.Effect = _blur;
            DetailOverlay.Visibility = Visibility.Visible;
            DetailOverlay.Focus();

            var dur = TimeSpan.FromMilliseconds(400);
            _blur.BeginAnimation(BlurEffect.RadiusProperty,
                new DoubleAnimation(0, BlurRadius, dur) { EasingFunction = _easeOut });
            DimBg.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, dur) { EasingFunction = _easeOut });
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.92, 1, dur) { EasingFunction = _easeOut });
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.92, 1, dur) { EasingFunction = _easeOut });
            DetailCard.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, dur) { EasingFunction = _easeOut });
        }

        private void CloseDetail()
        {
            var dur = TimeSpan.FromMilliseconds(320);
            _blur?.BeginAnimation(BlurEffect.RadiusProperty,
                new DoubleAnimation(0, dur) { EasingFunction = _easeOut });
            DimBg.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, dur) { EasingFunction = _easeOut });
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.94, dur) { EasingFunction = _easeOut });
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.94, dur) { EasingFunction = _easeOut });
            var fade = new DoubleAnimation(0, dur) { EasingFunction = _easeOut };
            fade.Completed += OnCloseCompleted;
            DetailCard.BeginAnimation(OpacityProperty, fade);
        }

        private void OnCloseCompleted(object? s, EventArgs e)
        {
            ClearDetailAnims();
            DetailOverlay.Visibility = Visibility.Collapsed;
            MainContentGrid.Effect = null; _blur = null;
            DetailCard.Opacity = 0;
            CardScale.ScaleX = 0.92; CardScale.ScaleY = 0.92;
            DimBg.Opacity = 0;
            if (VM is not null) { VM.IsDetailOpen = false; VM.DetailItem = null; }
        }

        private void ClearDetailAnims()
        {
            DetailCard.BeginAnimation(OpacityProperty, null);
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            DimBg.BeginAnimation(OpacityProperty, null);
            _blur?.BeginAnimation(BlurEffect.RadiusProperty, null);
        }

        private void OnDimClick(object s, MouseButtonEventArgs e) => CloseDetail();
        private void OnCloseDetail(object s, RoutedEventArgs e) => CloseDetail();
        private void OnOverlayKey(object s, KeyEventArgs e)
        { if (e.Key == Key.Escape) { CloseDetail(); e.Handled = true; } }

        // ══════════════════════════════════════════════════════════
        //   SETTINGS DRAWER
        // ══════════════════════════════════════════════════════════

        private void OnOpenSettings(object sender, RoutedEventArgs e) => OpenSettings();
        private void OnCloseSettings(object sender, RoutedEventArgs e) => CloseSettings();
        private void OnSettingsDimClick(object sender, MouseButtonEventArgs e) => CloseSettings();
        private void OnSettingsOverlayKey(object sender, KeyEventArgs e)
        { if (e.Key == Key.Escape) { CloseSettings(); e.Handled = true; } }

        private void OpenSettings()
        {
            ClearSettingsAnims();

            SettingsDim.Opacity = 0;
            DrawerSlide.X = 340;

            _settingsBlur = new BlurEffect { Radius = 0, RenderingBias = RenderingBias.Performance };
            MainContentGrid.Effect = _settingsBlur;

            SettingsOverlay.Visibility = Visibility.Visible;
            SettingsOverlay.Focus();

            var dur = TimeSpan.FromMilliseconds(380);

            _settingsBlur.BeginAnimation(BlurEffect.RadiusProperty,
                new DoubleAnimation(0, BlurRadius, dur) { EasingFunction = _easeOut });
            SettingsDim.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, dur) { EasingFunction = _easeOut });
            DrawerSlide.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(340, 0, dur) { EasingFunction = _easeOut });
        }

        private void CloseSettings()
        {
            var dur = TimeSpan.FromMilliseconds(300);

            _settingsBlur?.BeginAnimation(BlurEffect.RadiusProperty,
                new DoubleAnimation(0, dur) { EasingFunction = _easeOut });
            SettingsDim.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, dur) { EasingFunction = _easeOut });

            var slide = new DoubleAnimation(340, dur) { EasingFunction = _easeOut };
            slide.Completed += OnSettingsCloseCompleted;
            DrawerSlide.BeginAnimation(TranslateTransform.XProperty, slide);
        }

        private void OnSettingsCloseCompleted(object? s, EventArgs e)
        {
            ClearSettingsAnims();
            SettingsOverlay.Visibility = Visibility.Collapsed;
            MainContentGrid.Effect = null;
            _settingsBlur = null;
            SettingsDim.Opacity = 0;
            DrawerSlide.X = 340;
        }

        private void ClearSettingsAnims()
        {
            SettingsDim.BeginAnimation(OpacityProperty, null);
            DrawerSlide.BeginAnimation(TranslateTransform.XProperty, null);
            _settingsBlur?.BeginAnimation(BlurEffect.RadiusProperty, null);
        }

        // ══════════════════════════════════════════════════════════
        //   SELECTION BAR / DELETE BUTTON
        // ══════════════════════════════════════════════════════════

        private void AnimateBar(bool show)
        {
            if (show == _barShown) return; _barShown = show;
            BarSlide.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(show ? 0 : 120,
                    TimeSpan.FromMilliseconds(show ? 400 : 280))
                { EasingFunction = _easeOut });
            if (!show) AnimateDelBtn(false);
        }

        private void AnimateDelBtn(bool show)
        {
            if (show == _delShown) return; _delShown = show;
            IEasingFunction ease = show
                ? new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
                : _easeOut;
            var dur = TimeSpan.FromMilliseconds(show ? 320 : 200);
            double t = show ? 1 : 0;
            DelBtnScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(t, dur) { EasingFunction = ease });
            DelBtnScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(t, dur) { EasingFunction = ease });
            ExpSelScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(t, dur) { EasingFunction = ease });
            ExpSelScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(t, dur) { EasingFunction = ease });
        }

        // ══════════════════════════════════════════════════════════
        //   DETAIL ACTIONS
        // ══════════════════════════════════════════════════════════

        private void OnCopyField(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string f) return;
            if (VM?.DetailItem is null) return;
            string? v = f switch
            {
                "UUID"     => VM.DetailItem.UUID,
                "RefToken" => VM.DetailItem.RefToken,
                "AccToken" => VM.DetailItem.AccToken,
                _ => null
            };
            if (string.IsNullOrEmpty(v) || v == "N/A")
            { _snack.Enqueue("Field is empty"); return; }
            Copy(v, f);
        }

        private void OnCopyAll(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null) return;
            var d = VM.DetailItem;
            Copy($"IGN: {d.IGN}\nUUID: {d.UUID}\nClient ID: {d.ClientId}\n" +
                 $"Refresh Token: {d.RefToken}\nAccess Token: {d.AccToken}\n" +
                 $"Login Date: {d.LoginDate}", "All fields");
        }

        private async void OnDetailRefresh(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null || RootVM is null) return;
            string rf = VM.DetailItem.RefToken;
            string cid = VM.DetailItem.ClientId;
            var card = VM.DetailItem;

            if (string.IsNullOrEmpty(rf) || rf == "N/A")
            { _snack.Enqueue("No refresh token"); return; }

            CloseDetail();
            await Dispatcher.InvokeAsync(() => { },
                System.Windows.Threading.DispatcherPriority.Render);

            var conv = RootVM.Converter;
            conv.RefreshToken = rf;
            conv.SelectedClientIndex = CidToIdx(cid);

            if (Window.GetWindow(this) is MainWindow mw)
            {
                RootVM.SelectedNavIndex = 0;
                mw.NavListBoxControl.SelectedIndex = 0;
            }
            await conv.ConvertAsync(new Progress<string>(m => conv.StatusMessage = m));
            _ = card.RefreshHeadAsync();
        }

        // ══════════════════════════════════════════════════════════
        //   SETTINGS ACTIONS
        // ══════════════════════════════════════════════════════════

        private async void OnRefreshAllHeads(object sender, RoutedEventArgs e)
        {
            if (VM is null) return;
            _snack.Enqueue("Refreshing all head skins…");
            var items = VM.DisplayItems.ToList();
            var tasks = items.Select(i => i.RefreshHeadAsync()).ToList();
            await Task.WhenAll(tasks);
            _snack.Enqueue($"✓ Refreshed {items.Count} head(s)");
        }

        // ══════════════════════════════════════════════════════════
        //   SELECTION BUTTONS
        // ══════════════════════════════════════════════════════════

        private void OnSelectAll(object s, RoutedEventArgs e) => VM?.SelectAll();
        private void OnDeselectAll(object s, RoutedEventArgs e) => VM?.DeselectAll();
        private void OnDeleteSelected(object s, RoutedEventArgs e) => VM?.DeleteSelected();
        private void OnDeleteAll(object s, RoutedEventArgs e) => VM?.DeleteAll();

        // ══════════════════════════════════════════════════════════
        //   EXPORT / IMPORT
        // ══════════════════════════════════════════════════════════

        private void OnExportSelected(object s, RoutedEventArgs e)
        {
            if (VM is null) return;
            var sel = VM.SelectedProfiles();
            if (sel.Count == 0) { _snack.Enqueue("Nothing selected"); return; }
            DoExport(sel, $"selected_{DateTime.Now:yyyyMMdd_HHmmss}.tapf");
        }

        private void OnExportAll(object s, RoutedEventArgs e)
        {
            if (VM is null) return;
            var all = VM.AllProfiles();
            if (all.Count == 0) { _snack.Enqueue("No accounts to export"); return; }
            DoExport(all, $"alts_{DateTime.Now:yyyyMMdd_HHmmss}.tapf");
        }

        private void DoExport(List<ProfileDataBlock> list, string name)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export alt profiles", DefaultExt = ".tapf",
                Filter = "Token Alt Profile Files (*.tapf)|*.tapf", FileName = name
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                File.WriteAllBytes(dlg.FileName, ProfileService.ExportToBytes(list));
                _snack.Enqueue($"✓ Exported {list.Count} profile(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImport(object s, RoutedEventArgs e)
        {
            if (VM is null || RootVM is null) return;
            var dlg = new OpenFileDialog
            {
                Title = "Import alt profiles", DefaultExt = ".tapf",
                Filter = "Token Alt Profile Files (*.tapf)|*.tapf"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var imported = ProfileService.ImportFromBytes(File.ReadAllBytes(dlg.FileName));
                if (imported is null || imported.Count == 0)
                { _snack.Enqueue("File had no valid profiles"); return; }

                var choice = MessageBox.Show(
                    $"Found {imported.Count} profile(s).\n\nYES → Merge\nNO → Replace",
                    "Import mode", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Cancel) return;
                if (choice == MessageBoxResult.No) RootVM.TokenProfiles.Clear();

                foreach (var b in imported) RootVM.TokenProfiles.Add(b);
                var dd = ProfileService.RemoveDuplicates(RootVM.TokenProfiles.ToList());
                RootVM.TokenProfiles.Clear();
                foreach (var b in dd) RootVM.TokenProfiles.Add(b);

                VM.Save();
                _snack.Enqueue($"✓ Imported {imported.Count} profile(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        private void Copy(string val, string label)
        {
            try { Clipboard.SetText(val); _snack.Enqueue($"✓ Copied {label}"); }
            catch { _snack.Enqueue("Clipboard error"); }
        }

        private static int CidToIdx(string? n)
        {
            if (string.IsNullOrEmpty(n)) return 0;
            for (int i = 0; i < TokenConverterViewModel.ClientNames.Length; i++)
                if (string.Equals(TokenConverterViewModel.ClientNames[i], n,
                        StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }
    }
}
