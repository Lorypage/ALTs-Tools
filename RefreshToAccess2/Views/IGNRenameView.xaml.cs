using RefreshToAccess2.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views
{
    public partial class IGNRenameView : System.Windows.Controls.UserControl
    {
        private IGNRenameViewModel VM =>
            (IGNRenameViewModel)DataContext;

        public IGNRenameView()
        {
            InitializeComponent();
        }

        private async void OnRenameClicked(object sender, RoutedEventArgs e)
            => await VM.RenameAsync();
    }
}
