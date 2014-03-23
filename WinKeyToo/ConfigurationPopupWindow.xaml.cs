using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinKeyToo
{
    /// <summary>
    /// Interaction logic for ConfigurationPopupWindow.xaml
    /// </summary>
    public partial class ConfigurationPopupWindow : Window
    {
        public ConfigurationPopupWindow()
        {
            InitializeComponent();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
