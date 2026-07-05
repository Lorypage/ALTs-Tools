using RefreshToAccess2.Localization;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RefreshToAccess2.ViewModels
{
    public enum SortMode { DateNewest, DateOldest, NameAsc, NameDesc }

    public sealed class AltManagerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ProfileDataBlock> _master;
        private readonly Dictionary<ProfileDataBlock, ProfileCardItem> _cardCache = new();
        private DispatcherTimer? _debounceTimer;
        private DispatcherTimer? _headSaveTimer;

        public ObservableCollection<ProfileCardItem> DisplayItems { get; } = new();

        // ── Backing fields ─────────────────────────────────────────

        private string _searchText = "";
        private bool _isSelectionMode;
        private bool _isCardView = true;
        private ProfileCardItem? _detailItem;
        private bool _isDetailOpen;
        private int _selectionCount;
        private int _totalCount;
        private bool _isSearching;
        private SortMode _sortMode = SortMode.DateNewest;
        private double _cardWidth = 258;

        // ── Properties ─────────────────────────────────────────────

        public string SearchText
        {
            get => _searchText;
            set { SetField(ref _searchText, value); ScheduleFilter(); }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set => SetField(ref _isSearching, value);
        }

        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            set
            {
                SetField(ref _isSelectionMode, value);
                OnPropertyChanged(nameof(IsClickMode));
                if (!value) ClearSelections();
            }
        }

        public bool IsClickMode => !_isSelectionMode;

        public bool IsCardView
        {
            get => _isCardView;
            set { SetField(ref _isCardView, value); OnPropertyChanged(nameof(IsListView)); }
        }

        public bool IsListView => !_isCardView;

        public ProfileCardItem? DetailItem
        {
            get => _detailItem;
            set => SetField(ref _detailItem, value);
        }

        public bool IsDetailOpen
        {
            get => _isDetailOpen;
            set => SetField(ref _isDetailOpen, value);
        }

        public int SelectionCount
        {
            get => _selectionCount;
            private set
            {
                SetField(ref _selectionCount, value);
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SelectionLabel));
            }
        }

        public bool HasSelection => _selectionCount > 0;

        public string SelectionLabel =>
            _selectionCount > 0 ? Loc.T("AltMgr.DeleteCount", _selectionCount) : Loc.T("AltMgr.Delete");

        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                if (SetField(ref _totalCount, value))
                    OnPropertyChanged(nameof(AccountCountText));
            }
        }

        public string AccountCountText => Loc.T("AltMgr.AccountCount", _totalCount);

        /// <summary>Re-raises localized text properties after a language switch.</summary>
        public void RefreshLocalizedText()
        {
            OnPropertyChanged(nameof(SelectionLabel));
            OnPropertyChanged(nameof(AccountCountText));
        }

        public bool IsEmpty => DisplayItems.Count == 0;

        // ── Settings ───────────────────────────────────────────────

        public int SortIndex
        {
            get => (int)_sortMode;
            set
            {
                if (value < 0 || value > 3) return;
                _sortMode = (SortMode)value;
                OnPropertyChanged(nameof(SortIndex));
                ApplyFilterDirect();
            }
        }

        public double CardWidth
        {
            get => _cardWidth;
            set => SetField(ref _cardWidth, value);
        }

        // ── Constructor ────────────────────────────────────────────

        public AltManagerViewModel(ObservableCollection<ProfileDataBlock> master)
        {
            _master = master;
            _master.CollectionChanged += OnMasterChanged;
            ApplyFilterDirect();
        }

        private void OnMasterChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                _cardCache.Clear();
            else if (e.OldItems != null)
                foreach (ProfileDataBlock item in e.OldItems)
                    _cardCache.Remove(item);

            ApplyFilterDirect();
        }

        // ── Debounced search ───────────────────────────────────────

        private void ScheduleFilter()
        {
            IsSearching = true;
            if (_debounceTimer == null)
            {
                _debounceTimer = new DispatcherTimer(DispatcherPriority.Background)
                { Interval = TimeSpan.FromMilliseconds(300) };
                _debounceTimer.Tick += async (_, __) =>
                {
                    _debounceTimer.Stop();
                    await ApplyFilterAsync();
                };
            }
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async Task ApplyFilterAsync()
        {
            string query = (_searchText ?? "").Trim();
            var snapshot = _master.ToList();
            var mode = _sortMode;

            var filtered = await Task.Run(() =>
            {
                var list = string.IsNullOrEmpty(query)
                    ? snapshot
                    : snapshot.Where(p => Matches(p, query.ToLowerInvariant())).ToList();
                return Sort(list, mode);
            });

            RebuildDisplay(filtered);
            IsSearching = false;
        }

        private void ApplyFilterDirect()
        {
            string q = (_searchText ?? "").Trim().ToLowerInvariant();
            var source = string.IsNullOrEmpty(q)
                ? _master.ToList()
                : _master.Where(p => Matches(p, q)).ToList();

            RebuildDisplay(Sort(source, _sortMode));
            IsSearching = false;
        }

        private void RebuildDisplay(List<ProfileDataBlock> filtered)
        {
            // Unsubscribe old cards
            foreach (var c in DisplayItems)
            {
                c.SelectionChanged -= OnCardSel;
                c.HeadUpdated -= OnHeadUpdated;
            }

            DisplayItems.Clear();
            TotalCount = _master.Count;

            foreach (var p in filtered)
            {
                if (!_cardCache.TryGetValue(p, out var card))
                {
                    card = new ProfileCardItem(p);
                    _cardCache[p] = card;
                }
                card.SelectionChanged += OnCardSel;
                card.HeadUpdated += OnHeadUpdated;
                DisplayItems.Add(card);
            }

            RecountSelection();
            OnPropertyChanged(nameof(IsEmpty));
        }

        // ── Debounced save after head updates ──────────────────────

        private void OnHeadUpdated(object? sender, EventArgs e)
        {
            // Debounce: save 3 seconds after the last head update
            // so batch loads only trigger one save.
            if (_headSaveTimer == null)
            {
                _headSaveTimer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                _headSaveTimer.Tick += (_, __) =>
                {
                    _headSaveTimer.Stop();
                    Save();
                };
            }
            _headSaveTimer.Stop();
            _headSaveTimer.Start();
        }

        // ── Sorting ────────────────────────────────────────────────

        private static List<ProfileDataBlock> Sort(
            List<ProfileDataBlock> src, SortMode m) => m switch
        {
            SortMode.NameAsc    => src.OrderBy(p => p.profileData?.IGN ?? "",
                                       StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.NameDesc   => src.OrderByDescending(p => p.profileData?.IGN ?? "",
                                       StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.DateOldest => src.OrderBy(p => p.loginDate ?? "").ToList(),
            _                   => src.OrderByDescending(p => p.loginDate ?? "").ToList(),
        };

        // ── Public API ─────────────────────────────────────────────

        public List<ProfileDataBlock> AllProfiles()      => new(_master);
        public List<ProfileDataBlock> SelectedProfiles() =>
            DisplayItems.Where(i => i.IsSelected).Select(i => i.Block).ToList();

        public void SelectAll()   { foreach (var i in DisplayItems) i.IsSelected = true; }
        public void DeselectAll() => ClearSelections();

        public void DeleteSelected()
        {
            var sel = SelectedProfiles();
            if (sel.Count == 0) return;
            if (MessageBox.Show(Loc.T("AltMgr.ConfirmDeleteSelected", sel.Count),
                    Loc.T("Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;
            foreach (var s in sel) _master.Remove(s);
            Save();
        }

        public void DeleteAll()
        {
            if (_master.Count == 0) return;
            if (MessageBox.Show(Loc.T("AltMgr.ConfirmDeleteAll"),
                    Loc.T("Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;
            _master.Clear();
            RegistryService.Write("ProfileDataList", "");
        }

        public void Save() => ProfileService.Save(new List<ProfileDataBlock>(_master));

        /// <summary>
        /// Adds a freshly-created profile (e.g. from Microsoft login) to the
        /// master list, removes duplicates, and persists. Returns the de-duped
        /// block kept for this account so callers can act on it (e.g. load head).
        /// </summary>
        public ProfileDataBlock AddProfile(ProfileDataBlock block)
        {
            _master.Add(block);

            var deduped = ProfileService.RemoveDuplicates(_master.ToList());
            _master.Clear();
            foreach (var b in deduped) _master.Add(b);

            Save();

            // The kept block may be a different instance after de-dup; match by IGN.
            string ign = block.profileData?.IGN ?? "";
            return _master.FirstOrDefault(
                b => string.Equals(b.profileData?.IGN, ign,
                        StringComparison.OrdinalIgnoreCase)) ?? block;
        }

        /// <summary>
        /// "Logs in" a stored account: uses its refresh token to obtain a fresh
        /// Minecraft access token (the stored one expires after ~24h), writes the
        /// refreshed IGN / UUID / access token back into the block, and persists.
        /// </summary>
        /// <returns>The fresh access token.</returns>
        /// <exception cref="InvalidOperationException">No refresh token stored.</exception>
        public async Task<string> ActivateAsync(
            ProfileDataBlock block, IProgress<string>? progress = null)
        {
            var data = block.profileData
                ?? throw new InvalidOperationException(Loc.T("AltMgr.NoRefreshToken"));

            if (string.IsNullOrWhiteSpace(data.RefToken) || data.RefToken == "N/A")
                throw new InvalidOperationException(Loc.T("AltMgr.NoRefreshToken"));

            var client = ViewModels.TokenConverterViewModel.ResolveClientByName(data.ClientId);

            // [name, uuid, accessToken]
            string[] result = await MSLoginService.RequestTokenAsync(
                data.RefToken, client, progress);

            data.IGN      = result[0];
            data.UUID     = result[1];
            data.AccToken = result[2];

            Save();

            // Refresh the card's bound fields if it's currently displayed.
            if (_cardCache.TryGetValue(block, out var card))
                card.RaiseAllChanged();

            return result[2];
        }

        // ── Hypixel ban check ──────────────────────────────────────

        /// <summary>
        /// Refreshes the block's access token (from its refresh token), writes the
        /// fresh IGN / UUID / token back into the block, and returns the token.
        /// Does NOT persist — callers decide when to <see cref="Save"/>.
        /// </summary>
        private async Task<string> RefreshTokenAsync(
            ProfileDataBlock block, IProgress<string>? progress = null)
        {
            var data = block.profileData
                ?? throw new InvalidOperationException(Loc.T("AltMgr.NoRefreshToken"));

            if (string.IsNullOrWhiteSpace(data.RefToken) || data.RefToken == "N/A")
                throw new InvalidOperationException(Loc.T("AltMgr.NoRefreshToken"));

            var client = TokenConverterViewModel.ResolveClientByName(data.ClientId);
            string[] result = await MSLoginService.RequestTokenAsync(
                data.RefToken, client, progress);

            data.IGN      = result[0];
            data.UUID     = result[1];
            data.AccToken = result[2];
            return result[2];
        }

        /// <summary>
        /// Checks a single account's Hypixel ban status: refreshes its token, runs the
        /// login-protocol probe, writes the result into the block, persists, and
        /// refreshes the card. Never throws — failures are stored as an "error" status.
        /// </summary>
        public async Task CheckBanAsync(
            ProfileDataBlock block, IProgress<string>? progress = null)
        {
            _cardCache.TryGetValue(block, out var card);
            if (card != null) card.IsCheckingBan = true;
            try
            {
                HypixelBanService.BanResult result;
                try
                {
                    string token = await RefreshTokenAsync(block, progress);
                    result = await HypixelBanService.CheckAsync(
                        token, block.profileData!.UUID ?? "", block.profileData!.IGN ?? "");
                }
                catch (Exception ex)
                {
                    result = new HypixelBanService.BanResult(
                        HypixelBanService.StatusError, null, ex.Message);
                }

                ApplyResult(block, result);
                Save();
                card?.RaiseBanChanged();
            }
            finally
            {
                if (card != null) card.IsCheckingBan = false;
            }
        }

        /// <summary>
        /// Checks every stored account, at most <c>maxConcurrency</c> at a time to
        /// respect Hypixel / Mojang rate limits. Persists once at the end.
        /// </summary>
        public async Task CheckAllBansAsync(
            IProgress<(int done, int total)>? progress = null, int maxConcurrency = 3)
        {
            var blocks = _master.ToList();
            int total = blocks.Count;
            if (total == 0) return;

            int done = 0;
            using var gate = new System.Threading.SemaphoreSlim(maxConcurrency);

            var tasks = blocks.Select(async block =>
            {
                await gate.WaitAsync();
                _cardCache.TryGetValue(block, out var card);
                if (card != null)
                    await App.Current.Dispatcher.InvokeAsync(() => card.IsCheckingBan = true);
                try
                {
                    HypixelBanService.BanResult result;
                    try
                    {
                        string token = await RefreshTokenAsync(block);
                        result = await HypixelBanService.CheckAsync(
                            token, block.profileData!.UUID ?? "", block.profileData!.IGN ?? "");
                    }
                    catch (Exception ex)
                    {
                        result = new HypixelBanService.BanResult(
                            HypixelBanService.StatusError, null, ex.Message);
                    }

                    ApplyResult(block, result);
                    if (card != null)
                        await App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            card.IsCheckingBan = false;
                            card.RaiseBanChanged();
                        });
                }
                finally
                {
                    if (card != null)
                        await App.Current.Dispatcher.InvokeAsync(() => card.IsCheckingBan = false);
                    progress?.Report((System.Threading.Interlocked.Increment(ref done), total));
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            Save();
        }

        private static void ApplyResult(ProfileDataBlock block, HypixelBanService.BanResult r)
        {
            block.banStatus     = r.BanStatus;
            block.banDaysLeft   = r.DaysLeft;
            block.banRawMessage = r.RawMessage;
            block.banCheckedUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// Force an immediate save (call when navigating away, etc.).
        /// Stops any pending debounced head-save timer.
        /// </summary>
        public void FlushPendingSave()
        {
            if (_headSaveTimer is { IsEnabled: true })
            {
                _headSaveTimer.Stop();
                Save();
            }
        }

        // ── Internal ───────────────────────────────────────────────

        private void ClearSelections()
        { foreach (var i in DisplayItems) i.IsSelected = false; }

        private void OnCardSel(object? s, EventArgs e) => RecountSelection();

        private void RecountSelection() =>
            SelectionCount = DisplayItems.Count(i => i.IsSelected);

        private static bool Matches(ProfileDataBlock b, string q)
        {
            var d = b.profileData;
            if (d is null) return false;
            return Hit(d.IGN, q) || Hit(d.UUID, q) || Hit(d.ClientId, q)
                || Hit(d.RefToken, q) || Hit(d.AccToken, q) || Hit(b.loginDate, q);
        }

        private static bool Hit(string? v, string q) =>
            !string.IsNullOrEmpty(v) &&
            v.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
