using iNKORE.UI.WPF.Modern.Controls;
using Nes.Widget.Models;
using Nes.Widget.ViewModels;
using System.ComponentModel;
using System.Windows.Data;

namespace Nes.Widget.View
{
    /// <summary>
    /// SelectNesFileWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SelectNesFileWindow : ContentDialog
    {
        public SelectNesFileWindow( )
        {
            InitializeComponent( );
            DataContext = new SelectNesFileWindowVM( );
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ICollectionView cvs = CollectionViewSource.GetDefaultView(this.DataGrid.ItemsSource);
            if(cvs is not null && cvs.CanFilter)
            {
                cvs.Filter = (item) =>
                {
                    if(item is NesFileInfo info)
                    {
                        return info.Name.Contains(sender.Text);
                    }
                    return false;
                };
            }
        }
    }
}
