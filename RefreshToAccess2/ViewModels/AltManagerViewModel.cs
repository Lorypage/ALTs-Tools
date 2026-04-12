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
    public sealed class AltManagerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ProfileDataBlock> _master;
        private readonly Dictionary<ProfileDataBlock, ProfileCardItem> _cardCache = new();
        private DispatcherTimer? _debounceTimer;

        public ObservableCollection<ProfileCardItem> DisplayItems { get; } = new();

        private string _searchText = "";
        private bool _isSelectionMode;
        private bool _isCardView = true;
        private ProfileCardItem? _detailItem;
        private bool _isDetailOpen;
        private int _selectionCount;
        private int _totalCount;
        private bool _isSearching;

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetField(ref _searchText, value);
                ScheduleFilter();
            }
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
            set
            {
                SetField(ref _isCardView, value);
                OnPropertyChanged(nameof(IsListView));
            }
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
            _selectionCount > 0 ? $"Delete ({_selectionCount})" : "Delete";

        public int TotalCount
        {
            get => _totalCount;
            private set => SetField(ref _totalCount, value);
        }

        public bool IsEmpty => DisplayItems.Count == 0;

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
            // Show progress immediately on keystroke
            IsSearching = true;

            if (_debounceTimer == null)
            {
                _debounceTimer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
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

            List<ProfileDataBlock> filtered = await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(query))
                    return snapshot;
                string q = query.ToLowerInvariant();
                return snapshot.Where(p => Matches(p, q)).ToList();
            });

            RebuildDisplay(filtered);
            IsSearching = false;
        }

        private void ApplyFilterDirect()
        {
            string query = (_searchText ?? "").Trim().ToLowerInvariant();

            IEnumerable<ProfileDataBlock> source = string.IsNullOrEmpty(query)
                ? _master
                : _master.Where(p => Matches(p, query));

            RebuildDisplay(source.ToList());
            IsSearching = false;
        }

        private void RebuildDisplay(List<ProfileDataBlock> filtered)
        {
            foreach (var c in DisplayItems) c.SelectionChanged -= OnCardSel;

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
                DisplayItems.Add(card);
            }

            RecountSelection();
            OnPropertyChanged(nameof(IsEmpty));
        }

        // ── Public API ─────────────────────────────────────────────

        public List<ProfileDataBlock> AllProfiles() => new(_master);

        public List<ProfileDataBlock> SelectedProfiles() =>
            DisplayItems.Where(i => i.IsSelected).Select(i => i.Block).ToList();

        public void SelectAll()   { foreach (var i in DisplayItems) i.IsSelected = true; }
        public void DeselectAll() => ClearSelections();

        public void DeleteSelected()
        {
            var sel = SelectedProfiles();
            if (sel.Count == 0) return;
            if (MessageBox.Show(
                    $"Permanently delete {sel.Count} selected account(s)?",
                    "Confirm delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            foreach (var s in sel) _master.Remove(s);
            Save();
        }

        public void DeleteAll()
        {
            if (_master.Count == 0) return;
            if (MessageBox.Show(
                    "Permanently delete ALL stored accounts?",
                    "Confirm delete all",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            _master.Clear();
            RegistryService.Write("ProfileDataList", "");
        }

        public void Save() => ProfileService.Save(new List<ProfileDataBlock>(_master));

        // ── Internal ───────────────────────────────────────────────

        private void ClearSelections() { foreach (var i in DisplayItems) i.IsSelected = false; }
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
            !string.IsNullOrEmpty(v) && v.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
