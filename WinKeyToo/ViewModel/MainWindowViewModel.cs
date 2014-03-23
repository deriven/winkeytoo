using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinKeyToo.AttachedCommandBehavior;
using WinKeyToo.DataAccess;
using WinKeyToo.Instrumentation;
using WinKeyToo.Internals;

namespace WinKeyToo.ViewModel
{
    internal class MainWindowViewModel : WorkspaceViewModel
    {
        private SimpleCommand configureCommand;
        private SimpleCommand startCommand;
        private SimpleCommand stopCommand;
        private SimpleCommand restartCommand;
        private readonly DeviceMappingRepository deviceMappingRepository;
        private bool isStopping;
        private bool isConfiguring;

        public string DisplayMessage { get; set; }
        public ImageSource DisplayImage { get; set; }

        public override string DisplayName
        {
            get
            {
                return Strings.MainWindowViewModel_DisplayName;
            }
        }

        public ICommand ConfigureCommand
        {
            get
            {
                if (configureCommand == null)
                {
                    configureCommand = new SimpleCommand { ExecuteDelegate = Configure, CanExecuteDelegate = CanConfigure };
                }
                return configureCommand;
            }
        }

        public ICommand StartCommand
        {
            get
            {
                if (startCommand == null)
                {
                    startCommand = new SimpleCommand { ExecuteDelegate = Start, CanExecuteDelegate = CanStart };
                }
                return startCommand;
            }
        }

        public ICommand StopCommand
        {
            get
            {
                if (stopCommand == null)
                {
                    stopCommand = new SimpleCommand { ExecuteDelegate = Stop, CanExecuteDelegate = CanStop };
                }
                return stopCommand;
            }
        }

        public ICommand RestartCommand
        {
            get
            {
                if (restartCommand == null)
                {
                    restartCommand = new SimpleCommand { ExecuteDelegate = Restart, CanExecuteDelegate = CanRestart };
                }
                return restartCommand;
            }
        }

        public void Configure(object parameter)
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    isConfiguring = true;
                    Stop(parameter);
                    TheManager.Current.Clear();
                    var configurationWindow = new ConfigurationWindow
                                                  {
                                                      DataContext =
                                                          new ConfigurationWindowViewModel(deviceMappingRepository),
                                                      Owner = Application.Current.MainWindow
                                                  };
                    App.CurrentConfigurationWindow = configurationWindow;
                    configurationWindow.ShowDialog();
                    App.CurrentConfigurationWindow = null;
                    //deviceMappingRepository.Save();
                    Start(parameter);
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
                finally
                {
                    isConfiguring = false;
                }
            }
        }

        public bool CanConfigure(object parameter)
        {
            return !isConfiguring && App.CurrentConfigurationWindow == null && CanStop(parameter);
        }

        public void Start(object parameter)
        {
            InitializeMappings();
            TheManager.Current.Start();
            if (parameter == null) return;
            SetDisplayImage("green");
            DisplayMessage = "WinKeyToo started";
            base.OnPropertyChanged("DisplayMessage");
        }

        public bool CanStart(object parameter)
        {
            return !isConfiguring && App.CurrentConfigurationWindow == null && CanStop(parameter);
        }

        public void Stop(object parameter)
        {
            isStopping = true;
            TheManager.Current.Stop();
            isStopping = false;
            if (parameter == null) return;
            SetDisplayImage("gray");
            DisplayMessage = "WinKeyToo stopped";
            base.OnPropertyChanged("DisplayMessage");
        }

        public bool CanStop(object parameter)
        {
            return !isConfiguring && App.CurrentConfigurationWindow == null && !isStopping;
        }

        public void Restart(object parameter)
        {
            Stop(parameter);
            Start(parameter);
        }

        public bool CanRestart(object parameter)
        {
            return !isConfiguring && App.CurrentConfigurationWindow == null && CanStop(parameter);
        }

        protected override bool CanClose(object parameter)
        {
            return !isConfiguring && App.CurrentConfigurationWindow == null && CanStop(parameter);
        }

        public MainWindowViewModel(string deviceMappingDataFile)
        {
            deviceMappingRepository = new DeviceMappingRepository(deviceMappingDataFile);
        }

        ~MainWindowViewModel()
        {
            Stop(null);
        }

        private void SetDisplayImage(string iconName)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri("pack://application:,,/Icons/" + iconName + ".ico");
            img.EndInit();
            DisplayImage = img;
            base.OnPropertyChanged("DisplayImage");
        }

        private void InitializeMappings()
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    Stop(null);
                    TheManager.Current.Clear();
                    var deviceRepository = new DeviceMappingRepository(App.ConfigurationPath);
                    var deviceMappings = deviceRepository.GetDeviceMappings();

                    foreach (var mapping in deviceMappings)
                    {
                        if (!mapping.IsEnabled) continue;
                        var mapDevice = TheManager.Using.Device(mapping.DeviceInstanceGuid);
                        if (mapDevice == null)
                        {
                            App.CreateNotification(string.Format("{0} not found", mapping.DeviceType));
                            continue;
                        }
                        var mapSequence = mapDevice.StartNewSequence;
                        foreach (var input in mapping.InputCombination)
                        {
                            mapSequence.FollowedBy(input);
                        }
                        try
                        {
                            var deviceMapping = mapping;
                            var launchAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(
                                                     asm => Path.GetFileName(asm.Location) == deviceMapping.MappingActionAssemblyFileName)
                                                     .
                                                     SingleOrDefault() ??
                                                 Assembly.LoadFrom(mapping.MappingActionAssemblyFileName);
                            if (launchAssembly != null)
                            {
                                var type =
                                    launchAssembly.GetTypes().Where(
                                        t =>
                                        //!t.IsMarshalByRef ||
                                        t.FullName.Equals(deviceMapping.MappingActionTypeName,
                                                          StringComparison.OrdinalIgnoreCase)).
                                        SingleOrDefault();
                                if (type != null)
                                {
                                    var launchableType = launchAssembly.CreateInstance(type.FullName);
                                    var mapAction = launchableType as IMapActionPlugin;
                                    if (mapAction == null) continue;
                                    mapAction.Configuration = mapping.MappingActionConfiguration;
                                    mapSequence.To(mapAction);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            tracing.WriteError(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }
    }
}
