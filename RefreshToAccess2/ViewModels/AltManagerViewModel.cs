using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace RefreshToAccess2.ViewModels
{
    public sealed class AltManagerViewModel : ViewModelBase
    {
        // Reference to the shared master list owned by MainViewModel.
        private readonly ObservableCollection<ProfileDataBlock> _master;

        private string _searchText = "";
        private int _selectedIndex = -1;

        // Filtered view bound to the DataGrid.
        public ObservableCollection<ProfileDataBlock> Profiles { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetField(ref _searchText, value);
                ApplyFilter();
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetField(ref _selectedIndex, value);
        }

        public AltManagerViewModel(ObservableCollection<ProfileDataBlock> master)
        {
            _master = master;
            ApplyFilter();

            // Re-filter whenever the master list changes so new logins
            // appear immediately without manual refresh.
            _master.CollectionChanged += (s, e) => ApplyFilter();
        }

        // ── Public API ─────────────────────────────────────────────────

        public ProfileDataBlock? GetSelected()
        {
            if (SelectedIndex < 0 || SelectedIndex >= Profiles.Count)
                return null;
            return Profiles[SelectedIndex];
        }

        /// <summary>
        /// Returns a snapshot of the full (unfiltered) master list.
        /// Used by export and token-injection features.
        /// </summary>
        public List<ProfileDataBlock> AllProfiles()
            => new List<ProfileDataBlock>(_master);

        public void DeleteSelected()
        {
            var item = GetSelected();
            if (item is null)
            {
                MessageBox.Show(
                    "Please select an account first.",
                    "Nothing selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    "Permanently delete the selected account entry?",
                    "Confirm delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            _master.Remove(item);
            Save();
        }

        public void DeleteAll()
        {
            if (MessageBox.Show(
                    "Permanently delete ALL stored accounts?",
                    "Confirm delete all",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            _master.Clear();
            RegistryService.Write("ProfileDataList", "");
        }

        public void Save()
            => ProfileService.Save(new List<ProfileDataBlock>(_master));

        // ── Filtering ──────────────────────────────────────────────────

        private void ApplyFilter()
        {
            Profiles.Clear();

            var source = string.IsNullOrWhiteSpace(SearchText)
                ? _master
                : _master.Where(p => Matches(p, SearchText.Trim().ToLower()));

            foreach (var p in source)
                Profiles.Add(p);
        }

        private static bool Matches(ProfileDataBlock b, string q)
        {
            var d = b.profileData;
            if (d is null) return false;

            return Contains(d.IGN, q)
                || Contains(d.RefToken, q)
                || Contains(d.AccToken, q)
                || Contains(d.UUID, q)
                || Contains(d.ClientId, q)
                || Contains(b.loginDate, q);
        }

        private static bool Contains(string? value, string query)
            => !string.IsNullOrEmpty(value)
               && value.ToLower().Contains(query);
    }
}
