using RefreshToAccess2.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RefreshToAccess2.ViewModels
{
    public sealed class IGNRenameViewModel : ViewModelBase
    {
        private readonly TokenConverterViewModel _conv;

        private string _newName = "";
        private bool   _isBusy;

        public string NewName
        {
            get => _newName;
            set => SetField(ref _newName, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetField(ref _isBusy, value);
        }

        public IGNRenameViewModel(TokenConverterViewModel conv)
        {
            _conv   = conv;
            NewName = "";
        }

        public async Task RenameAsync()
        {
            if (NewName == _conv.ProfileName)
            {
                MessageBox.Show("The name hasn't changed.",
                    "No change", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_conv.AccessToken))
            {
                MessageBox.Show("No access token available – convert a token first.",
                    "Not logged in", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                await IGNRenameService.RenameAsync(NewName, _conv.AccessToken);
                _conv.ProfileName = NewName;
                MessageBox.Show($"Successfully renamed to: {NewName}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
