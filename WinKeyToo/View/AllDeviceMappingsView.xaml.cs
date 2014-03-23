using System.Windows;
using System.Windows.Controls;
using WinKeyToo.ViewModel;

namespace WinKeyToo.View
{
    /// <summary>
    /// Interaction logic for AllDeviceMappingsView.xaml
    /// </summary>
    public partial class AllDeviceMappingsView
    {
        public AllDeviceMappingsView()
        {
            InitializeComponent();
        }

        private void AllDeviceMappingsListViewLoaded(object sender, RoutedEventArgs e)
        {
            AllDeviceMappingsListView.SelectedIndex = 0;
        }

        private void AllDeviceMappingsListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deviceMappingViewModel = AllDeviceMappingsListView.SelectedItem as DeviceMappingViewModel;
            var deviceMappingView = new DeviceMappingView {DataContext = deviceMappingViewModel};
            SelectedDeviceMappingContentPresenter.Content = deviceMappingView;
        }
    }
}
