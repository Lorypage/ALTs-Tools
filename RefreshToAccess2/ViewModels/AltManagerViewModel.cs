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
