using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using WinKeyToo.AttachedCommandBehavior;
using WinKeyToo.DataAccess;
using WinKeyToo.Model;

namespace WinKeyToo.ViewModel
{
    internal class AllDeviceMappingsViewModel : WorkspaceViewModel
    {
        #region Fields

        readonly DeviceMappingRepository deviceMappingRepository;
        private SimpleCommand newDeviceMappingCommand;

        #endregion // Fields

        #region Constructor

        public AllDeviceMappingsViewModel(DeviceMappingRepository deviceMappingRepository)
        {
            if (deviceMappingRepository == null)
                throw new ArgumentNullException("deviceMappingRepository");

            base.DisplayName = Strings.AllDeviceMappingsViewModel_DisplayName;            

            this.deviceMappingRepository = deviceMappingRepository;

            // Subscribe for notifications of when a new deviceMapping is saved.
            this.deviceMappingRepository.DeviceMappingAdded += OnDeviceMappingAddedToRepository;

            // Subscribe for notifications of when a new deviceMapping is deleted.
            this.deviceMappingRepository.DeviceMappingRemoved += OnDeviceMappingRemovedFromRepository;

            // Populate the AllDeviceMappings collection with DeviceMappingViewModels.
            CreateAllDeviceMappings();              
        }

        void CreateAllDeviceMappings()
        {
            var all =
                (from devMap in deviceMappingRepository.GetDeviceMappings()
                 select new DeviceMappingViewModel(devMap, deviceMappingRepository)).ToList();

            foreach (var dmvm in all)
                dmvm.PropertyChanged += OnDeviceMappingViewModelPropertyChanged;

            AllDeviceMappings = new ObservableCollection<DeviceMappingViewModel>(all);
            AllDeviceMappings.CollectionChanged += OnCollectionChanged;
        }

        #endregion // Constructor

        #region Public Properties
        public ICommand NewDeviceMappingCommand
        {
            get
            {
                if (newDeviceMappingCommand == null)
                {
                    newDeviceMappingCommand = new SimpleCommand { ExecuteDelegate = CreateDeviceMapping, CanExecuteDelegate = CanCreateNewDeviceMapping };
                }
                return newDeviceMappingCommand;
            }
        }
        #endregion // Public Properties

        #region Public Methods
        public void CreateDeviceMapping(object parameter)
        {
            var newDeviceMapping = DeviceMapping.CreateNewDeviceMapping();
            deviceMappingRepository.AddDeviceMapping(newDeviceMapping);
            var listView = (parameter as ListView);
            if (listView == null) return;
            foreach (var devMapViewModel in AllDeviceMappings)
            {
                if (devMapViewModel.DeviceMapping != newDeviceMapping) continue;
                listView.SelectedItem = devMapViewModel;
                break;
            }
        }

        public bool CanCreateNewDeviceMapping(object parameter)
        {
            return true;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Returns a collection of all the DeviceMappingViewModel objects.
        /// </summary>
        public ObservableCollection<DeviceMappingViewModel> AllDeviceMappings { get; private set; }

        #endregion // Public Interface

        #region  Base Class Overrides

        protected override void OnDispose()
        {
            foreach (var devMapVM in AllDeviceMappings)
                devMapVM.Dispose();

            AllDeviceMappings.Clear();
            AllDeviceMappings.CollectionChanged -= OnCollectionChanged;

            deviceMappingRepository.DeviceMappingAdded -= OnDeviceMappingAddedToRepository;
        }

        #endregion // Base Class Overrides

        #region Event Handling Methods

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (DeviceMappingViewModel devMapVM in e.NewItems)
                    devMapVM.PropertyChanged += OnDeviceMappingViewModelPropertyChanged;

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (DeviceMappingViewModel devMapVM in e.OldItems)
                    devMapVM.PropertyChanged -= OnDeviceMappingViewModelPropertyChanged;
        }

        void OnDeviceMappingViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            const string isSelected = "IsSelected";

            // Make sure that the property name we're referencing is valid.
            // This is a debugging technique, and does not execute in a Release build.
            ((DeviceMappingViewModel) sender).VerifyPropertyName(isSelected);

            // When a deviceMapping is selected or unselected, we must let the
            // world know that the TotalSelectedSales property has changed,
            // so that it will be queried again for a new value.
            //if (e.PropertyName == isSelected)
            //    OnPropertyChanged("TotalMappings");
        }

        void OnDeviceMappingAddedToRepository(object sender, DeviceMappingEventArgs e)
        {
            var viewModel = new DeviceMappingViewModel(e.DeviceMapping, deviceMappingRepository);
            AllDeviceMappings.Add(viewModel);
        }

        void OnDeviceMappingRemovedFromRepository(object sender, DeviceMappingEventArgs e)
        {
            var viewModel = (from vm in AllDeviceMappings where vm.DeviceMapping == e.DeviceMapping select vm).SingleOrDefault();
            if (viewModel != null) AllDeviceMappings.Remove(viewModel);
        }

        #endregion // Event Handling Methods
    }
}