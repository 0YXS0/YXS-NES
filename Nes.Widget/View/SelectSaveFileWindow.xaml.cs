using iNKORE.UI.WPF.Modern.Controls;
using Nes.Widget.ViewModels;

namespace Nes.Widget.View
{
    /// <summary>
    /// SelectSaveFileWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SelectSaveFileWindow : ContentDialog
    {
        private object? LastSelectedItem;
        private readonly SelectSaveFileWindowVM m_VM = new( );

        public SelectSaveFileWindow( )
        {
            InitializeComponent( );
            this.DataContext = m_VM;
            this.gridView.ItemsSource = m_VM.SaveInfos;
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if(e.ClickedItem == LastSelectedItem)
            {
                LastSelectedItem = null;
                if(sender is GridView t)
                {
                    m_VM.SelectedSaveInfoIndex = t.SelectedIndex;
                }
            }
            else
                LastSelectedItem = e.ClickedItem;
        }
    }
}
