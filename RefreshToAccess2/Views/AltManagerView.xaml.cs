using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using RefreshToAccess2.Localization;
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

        // Last selected detail tab index, for deciding page-flip slide direction.
        private int _lastTabIndex = -1;

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

            // Reset tab state so the first tab shows without a switch transition,
            // and default to the Account tab on each open.
            _lastTabIndex = -1;
            if (DetailTabs != null) DetailTabs.SelectedIndex = 0;

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
            { _snack.Enqueue(Loc.T("AltMgr.FieldEmpty")); return; }
            Copy(v, f);
        }

        private void OnCopyAll(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null) return;
            var d = VM.DetailItem;
            Copy(Loc.T("AltMgr.CopyAllBody", d.IGN, d.UUID, d.ClientId, d.RefToken, d.AccToken, d.LoginDate),
                 Loc.T("AltMgr.CopyAll"));
        }

        private async void OnDetailRefresh(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null || RootVM is null) return;
            string rf = VM.DetailItem.RefToken;
            string cid = VM.DetailItem.ClientId;
            var card = VM.DetailItem;

            if (string.IsNullOrEmpty(rf) || rf == "N/A")
            { _snack.Enqueue(Loc.T("AltMgr.NoRefreshToken")); return; }

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
        //   LOGIN (activate account for inject + profile editing)
        // ══════════════════════════════════════════════════════════

        private bool _detailActionBusy;

        /// <summary>
        /// Logs the detail account in: refreshes its access token from the stored
        /// refresh token, writes it back to the profile, and pushes it into the
        /// Converter so BOTH the injector (reads stored profiles) and the skin /
        /// profile editor (reads Converter.AccessToken) can use it immediately —
        /// without navigating away from the Alt Manager.
        /// </summary>
        private async void OnDetailLogin(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null || RootVM is null || _detailActionBusy) return;
            _detailActionBusy = true;
            try
            {
                var card = VM.DetailItem;

                if (string.IsNullOrEmpty(card.RefToken) || card.RefToken == "N/A")
                { _snack.Enqueue(Loc.T("AltMgr.NoRefreshToken")); return; }

                _snack.Enqueue(Loc.T("AltMgr.LoggingIn"));

                string token;
                try
                {
                    token = await VM.ActivateAsync(card.Block,
                        new Progress<string>(m => _snack.Enqueue(m)));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Loc.T("AltMgr.LoginFailed", ex.Message),
                        Loc.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Make this the "current" account for the skin / profile editor.
                // The injector reads the refreshed token straight from the
                // stored profile list (updated by ActivateAsync above).
                var conv = RootVM.Converter;
                conv.AccessToken = token;
                conv.ProfileName = card.IGN;
                conv.PlayerUuid  = card.UUID;
                conv.LoggedIn    = true;

                _ = card.RefreshHeadAsync();
                _snack.Enqueue(Loc.T("AltMgr.LoginSuccess", card.IGN));
            }
            finally
            {
                _detailActionBusy = false;
            }
        }

        /// <summary>
        /// Checks the detail account's Hypixel ban status. The badge on the card and the
        /// detail overlay update in place via the card's change notifications.
        /// </summary>
        private async void OnCheckBan(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null || _detailActionBusy) return;
            var card = VM.DetailItem;

            if (string.IsNullOrEmpty(card.RefToken) || card.RefToken == "N/A")
            { _snack.Enqueue(Loc.T("AltMgr.NoRefreshToken")); return; }

            _detailActionBusy = true;
            try
            {
                _snack.Enqueue(Loc.T("AltMgr.Checking"));
                await VM.CheckBanAsync(card.Block,
                    new Progress<string>(m => _snack.Enqueue(m)));
                _snack.Enqueue(card.BanBadgeText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Loc.T("AltMgr.BanCheckFailed", ex.Message),
                    Loc.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _detailActionBusy = false;
            }
        }

        /// <summary>
        /// On switching between the Account / Hypixel tabs: slides the new tab's content
        /// in horizontally (page-flip — direction follows tab order) and smoothly tweens
        /// the detail card's height to fit the new content so it doesn't jump.
        /// </summary>
        private void OnDetailTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is not System.Windows.Controls.TabControl tc) return;
            if (tc.SelectedContent is not FrameworkElement content) return;

            int newIndex = tc.SelectedIndex;
            int prevIndex = _lastTabIndex;
            _lastTabIndex = newIndex;

            // First selection when the overlay opens: no transition.
            if (prevIndex < 0) return;

            // Direction: moving to a later tab slides in from the right, earlier from the left.
            double from = newIndex > prevIndex ? 46 : -46;

            var slide = new TranslateTransform(from, 0);
            content.RenderTransform = slide;
            content.Opacity = 0;

            var dur = TimeSpan.FromMilliseconds(320);
            content.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, dur) { EasingFunction = _easeOut });
            slide.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(from, 0, dur) { EasingFunction = _easeOut });

            AnimateCardHeightToContent();
        }

        /// <summary>
        /// Tweens <see cref="DetailCard"/>'s Height from its current rendered height to
        /// the height it wants for the freshly selected tab, so the card grows/shrinks
        /// smoothly instead of snapping.
        /// </summary>
        private void AnimateCardHeightToContent()
        {
            double current = DetailCard.ActualHeight;
            if (current <= 0) return;

            // Read the ACCURATE natural height for the new content: drop any animation,
            // let the card size to Auto, force a real layout pass, then read ActualHeight.
            // (Measure/DesiredSize alone is stale mid-switch and causes an end-frame jump.)
            DetailCard.BeginAnimation(FrameworkElement.HeightProperty, null);
            DetailCard.ClearValue(FrameworkElement.HeightProperty);
            DetailCard.UpdateLayout();
            double target = DetailCard.ActualHeight;

            if (Math.Abs(target - current) < 1) return;

            // Restore the starting height, then animate to the measured target. On
            // completion, release to Auto — which now equals target, so no jump.
            DetailCard.Height = current;
            var anim = new DoubleAnimation(current, target,
                TimeSpan.FromMilliseconds(320)) { EasingFunction = _easeOut };
            anim.Completed += (_, __) =>
            {
                DetailCard.BeginAnimation(FrameworkElement.HeightProperty, null);
                DetailCard.ClearValue(FrameworkElement.HeightProperty);
            };
            DetailCard.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }

        /// <summary>Loads Hypixel stats for the detail account (khadow.lol API).</summary>
        private async void OnLoadStats(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null) return;
            await VM.DetailItem.LoadStatsAsync();
        }

        /// <summary>Force-refreshes the currently displayed Hypixel stats.</summary>
        private async void OnRefreshStats(object sender, RoutedEventArgs e)
        {
            if (VM?.DetailItem is null) return;
            await VM.DetailItem.LoadStatsAsync(force: true);
        }

        // ══════════════════════════════════════════════════════════
        //   SETTINGS ACTIONS
        // ══════════════════════════════════════════════════════════

        private async void OnRefreshAllHeads(object sender, RoutedEventArgs e)
        {
            if (VM is null) return;
            _snack.Enqueue(Loc.T("AltMgr.RefreshingHeads"));
            var items = VM.DisplayItems.ToList();
            var tasks = items.Select(i => i.RefreshHeadAsync()).ToList();
            await Task.WhenAll(tasks);
            _snack.Enqueue(Loc.T("AltMgr.RefreshedHeads", items.Count));
        }

        private bool _checkAllBusy;

        private async void OnCheckAllBans(object sender, RoutedEventArgs e)
        {
            if (VM is null || _checkAllBusy) return;
            _checkAllBusy = true;
            try
            {
                _snack.Enqueue(Loc.T("AltMgr.Checking"));
                var progress = new Progress<(int done, int total)>(p =>
                    _snack.Enqueue(Loc.T("AltMgr.CheckingProgress", p.done, p.total)));
                int total = VM.AllProfiles().Count;
                await VM.CheckAllBansAsync(progress);
                _snack.Enqueue(Loc.T("AltMgr.CheckedAllBans", total));
            }
            finally
            {
                _checkAllBusy = false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //   SELECTION BUTTONS
        // ══════════════════════════════════════════════════════════

        private void OnSelectAll(object s, RoutedEventArgs e) => VM?.SelectAll();
        private void OnDeselectAll(object s, RoutedEventArgs e) => VM?.DeselectAll();
        private void OnDeleteSelected(object s, RoutedEventArgs e) => VM?.DeleteSelected();
        private void OnDeleteAll(object s, RoutedEventArgs e) => VM?.DeleteAll();

        // ══════════════════════════════════════════════════════════
        //   MICROSOFT LOGIN
        // ══════════════════════════════════════════════════════════

        private bool _msLoginBusy;

        private async void OnMicrosoftLogin(object sender, RoutedEventArgs e)
        {
            if (VM is null || _msLoginBusy) return;
            _msLoginBusy = true;
            try
            {
                // Vanilla legacy MSA client → produces M.C5... refresh tokens.
                var client = ClientIdentification.Vanilla;

                var win = new Dialogs.MicrosoftLoginWindow(client)
                {
                    Owner = Window.GetWindow(this)
                };

                bool? ok = win.ShowDialog();
                if (ok != true || string.IsNullOrEmpty(win.AuthCode))
                {
                    _snack.Enqueue(Loc.T("AltMgr.MsLoginCancelled"));
                    return;
                }

                _snack.Enqueue(Loc.T("AltMgr.MsLoginProgress"));

                // code → refresh token → full Minecraft auth chain
                string refresh = await MSLoginService.ExchangeCodeForRefreshTokenAsync(
                    win.AuthCode, client);

                string[] result = await MSLoginService.RequestTokenAsync(refresh, client);

                var block = new ProfileDataBlock
                {
                    loginDate   = DateTime.Now.ToString(@"yyyy/MM/dd HH:mm:ss"),
                    profileData = new ProfileData
                    {
                        IGN      = result[0],
                        UUID     = result[1],
                        RefToken = refresh,
                        AccToken = result[2],
                        ClientId = "Vanilla"
                    }
                };

                var kept = VM.AddProfile(block);
                _snack.Enqueue(Loc.T("AltMgr.MsLoginSuccess", result[0]));

                // Fetch the head skin for the new card in the background.
                if (VM.DisplayItems.FirstOrDefault(i => i.Block == kept) is { } card)
                    _ = card.RefreshHeadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Loc.T("AltMgr.MsLoginFailed", ex.Message),
                    Loc.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _msLoginBusy = false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //   EXPORT / IMPORT
        // ══════════════════════════════════════════════════════════

        private void OnExportSelected(object s, RoutedEventArgs e)
        {
            if (VM is null) return;
            var sel = VM.SelectedProfiles();
            if (sel.Count == 0) { _snack.Enqueue(Loc.T("AltMgr.NothingSelected")); return; }
            DoExport(sel, $"selected_{DateTime.Now:yyyyMMdd_HHmmss}.tapf");
        }

        private void OnExportAll(object s, RoutedEventArgs e)
        {
            if (VM is null) return;
            var all = VM.AllProfiles();
            if (all.Count == 0) { _snack.Enqueue(Loc.T("AltMgr.NoAccountsToExport")); return; }
            DoExport(all, $"alts_{DateTime.Now:yyyyMMdd_HHmmss}.tapf");
        }

        private void DoExport(List<ProfileDataBlock> list, string name)
        {
            var dlg = new SaveFileDialog
            {
                Title = Loc.T("AltMgr.ExportTitle"), DefaultExt = ".tapf",
                Filter = "Token Alt Profile Files (*.tapf)|*.tapf", FileName = name
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                File.WriteAllBytes(dlg.FileName, ProfileService.ExportToBytes(list));
                _snack.Enqueue(Loc.T("AltMgr.Exported", list.Count));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Loc.T("AltMgr.ExportFailed", ex.Message), Loc.T("Common.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImport(object s, RoutedEventArgs e)
        {
            if (VM is null || RootVM is null) return;
            var dlg = new OpenFileDialog
            {
                Title = Loc.T("AltMgr.ImportTitle"), DefaultExt = ".tapf",
                Filter = "Token Alt Profile Files (*.tapf)|*.tapf"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var imported = ProfileService.ImportFromBytes(File.ReadAllBytes(dlg.FileName));
                if (imported is null || imported.Count == 0)
                { _snack.Enqueue(Loc.T("AltMgr.NoValidProfiles")); return; }

                var choice = MessageBox.Show(
                    Loc.T("AltMgr.ImportMode", imported.Count),
                    Loc.T("AltMgr.ImportModeTitle"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Cancel) return;
                if (choice == MessageBoxResult.No) RootVM.TokenProfiles.Clear();

                foreach (var b in imported) RootVM.TokenProfiles.Add(b);
                var dd = ProfileService.RemoveDuplicates(RootVM.TokenProfiles.ToList());
                RootVM.TokenProfiles.Clear();
                foreach (var b in dd) RootVM.TokenProfiles.Add(b);

                VM.Save();
                _snack.Enqueue(Loc.T("AltMgr.Imported", imported.Count));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Loc.T("AltMgr.ImportFailed", ex.Message), Loc.T("Common.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        private void Copy(string val, string label)
        {
            if (Clipboard.TrySetText(val)) _snack.Enqueue(Loc.T("AltMgr.Copied", label));
            else _snack.Enqueue(Loc.T("AltMgr.ClipboardError"));
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
