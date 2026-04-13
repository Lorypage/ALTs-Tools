using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace RefreshToAccess2.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<ProfileDataBlock> TokenProfiles { get; }

        public TokenConverterViewModel Converter   { get; }
        public AltManagerViewModel     AltManager  { get; }
        public IGNRenameViewModel      IGNRename   { get; }
        public SkinChangerViewModel    SkinChanger { get; }

        public int SelectedNavIndex { get; set; }

        public MainViewModel()
        {
            var saved = ProfileService.Load();
            TokenProfiles = new ObservableCollection<ProfileDataBlock>(ProfileService.Load() ?? new List<ProfileDataBlock>());
            Converter   = new TokenConverterViewModel();
            AltManager  = new AltManagerViewModel(TokenProfiles);
            IGNRename   = new IGNRenameViewModel(Converter);
            SkinChanger = new SkinChangerViewModel(Converter);

            Converter.OnProfileAdded += block =>
            {
                TokenProfiles.Add(block);

                var deduped = ProfileService.RemoveDuplicates(TokenProfiles.ToList());

                TokenProfiles.Clear();
                foreach (var b in deduped)
                    TokenProfiles.Add(b);

                ProfileService.Save(TokenProfiles.ToList());
            };
        }
    }
}
