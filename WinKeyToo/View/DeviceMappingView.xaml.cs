using System.Windows;
using System.Windows.Controls;
using WinKeyToo.ViewModel;

namespace WinKeyToo.View
{
    /// <summary>
    /// Interaction logic for DeviceMappingView.xaml
    /// </summary>
    public partial class DeviceMappingView
    {
        public DeviceMappingView()
        {
            InitializeComponent();
        }

        private void DeviceMappingTypeCmbLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as DeviceMappingViewModel; // had to do it .. :(
            if (viewModel != null) deviceMappingTypeCmb.SelectedItem =  viewModel.SelectedDevice;
        }

        private void DeviceMappingActionCmdLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as DeviceMappingViewModel; // had to do it .. :(
            if (viewModel != null) deviceMappingActionCmb.SelectedItem = viewModel.SelectedAction;
        }
    }
}
