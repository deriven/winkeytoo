using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Windows.Markup;
using WinKeyToo.ViewModel;

namespace WinKeyToo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        internal static string ConfigurationPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                    "Combination.xml");
            }
        }

        internal static void CreateNotification(string message)
        {
            var mainWindow = Current.MainWindow as MainWindow;
            if (mainWindow != null) mainWindow.AddNotification(message);
        }

        internal static bool FirstTimeRun { get; set; }
        internal static bool ShutdownInstances { get; set; }

        static App()
        {
            // This code is used to test the app when using other cultures.
            //
            //System.Threading.Thread.CurrentThread.CurrentCulture =
            //    System.Threading.Thread.CurrentThread.CurrentUICulture =
            //        new System.Globalization.CultureInfo("it-IT");


            // Ensure the current culture passed into bindings is the OS culture.
            // By default, WPF uses en-US as the culture, regardless of the system settings.
            //
            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //if (ApplicationDeployment.IsNetworkDeployed && ApplicationDeployment.CurrentDeployment.IsFirstRun) ConfigurationWindowViewModel.CreateWindowsStartup(true);
        }

        public static ConfigurationWindow CurrentConfigurationWindow { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Application is running
            // Process command line args
            FirstTimeRun = false;
            for (var i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "1TI")
                {
                    FirstTimeRun = true;
                }
                if (e.Args[i] == "SDI")
                {
                    ShutdownInstances = true;
                }
            }

            // Get Reference to the current Process
            var thisProc = Process.GetCurrentProcess();
            // Check how many total processes have the same name as the current one
            if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1
                || ShutdownInstances)
            {
                if (ShutdownInstances)
                {
                    var instances = Process.GetProcessesByName(thisProc.ProcessName);
                    for (var index = 0; index < instances.Length; index++)
                    {
                        instances[index].Close();
                    }
                }
                else
                {
                    // If there is more than one, than it is already running.
                    MessageBox.Show("WinKeyToo is already running.");
                }
                Current.Shutdown();
                return;
            }

            // Create the ViewModel to which 
            // the main window binds.
            var path = ConfigurationPath;
            var viewModel = new MainWindowViewModel(path);
            // Allow all controls in the window to 
            // bind to the ViewModel by setting the 
            // DataContext, which propagates down 
            // the element tree.
            var window = new MainWindow(viewModel);
            MainWindow = window;

            // When the ViewModel asks to be closed, 
            // close the window.
            EventHandler handler = null;
            var close = handler;
            handler = delegate
            {
                viewModel.RequestClose -= close;
                window.Close();
            };
            viewModel.RequestClose += handler;

            //window.DataContext = viewModel;
            viewModel.Start(window);
            window.Show();

            //// Create main application window, starting minimized if specified
            //var mainWindow = new MainWindow();
            //MainWindow = mainWindow;
            ////mainWindow.DataContext = new WinKeyTooViewModel();
            //mainWindow.Show();
        }
    }
}
