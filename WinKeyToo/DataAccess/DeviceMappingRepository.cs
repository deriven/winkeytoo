using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using WinKeyToo.Model;

namespace WinKeyToo.DataAccess
{
    internal class DeviceMappingRepository
    {
        #region Fields

        private readonly DeviceMappingList deviceMappings;
        private readonly string deviceMappingDataFile;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Creates a new repository of deviceMappings.
        /// </summary>
        /// <param name="deviceMappingDataFile">The relative path to an XML resource file that contains deviceMapping data.</param>
        public DeviceMappingRepository(string deviceMappingDataFile)
        {
            this.deviceMappingDataFile = deviceMappingDataFile;
            deviceMappings = LoadChanges<DeviceMappingList>(deviceMappingDataFile) ?? new DeviceMappingList();
        }

        #endregion // Constructor

        #region Destructor
        ~DeviceMappingRepository()
        {
        }
        #endregion // Destructor

        #region Public Interface

        /// <summary>
        /// Raised when a deviceMapping is placed into the repository.
        /// </summary>
        public event EventHandler<DeviceMappingEventArgs> DeviceMappingAdded;
        public event EventHandler<DeviceMappingEventArgs> DeviceMappingRemoved;

        public void SaveAll()
        {
            RecordChanges<DeviceMappingList>(deviceMappingDataFile, deviceMappings.SerializableItems.GetEnumerator());
        }

        public void Save(Guid id)
        {
            var currentMapping = (from m in deviceMappings where m.Id.Equals(id) select m).SingleOrDefault();
            if (currentMapping == null) return;
            var mappings = LoadChanges<DeviceMappingList>(deviceMappingDataFile) ?? new DeviceMappingList();
            var mapping = (from m in mappings where m.Id.Equals(id) select m).SingleOrDefault();
            if (mapping != null) mappings.Remove(mapping);
            mappings.Add(currentMapping);
            SaveAll();
        }

        /// <summary>
        /// Places the specified deviceMapping into the repository.
        /// If the deviceMapping is already in the repository, an
        /// exception is not thrown.
        /// </summary>
        public void AddDeviceMapping(DeviceMapping deviceMapping)
        {
            if (deviceMapping == null)
                throw new ArgumentNullException("deviceMapping");

            if (deviceMappings.Contains(deviceMapping)) return;
            deviceMappings.Add(deviceMapping);

            var handler = DeviceMappingAdded;
            if (handler != null)
                handler(this, new DeviceMappingEventArgs(deviceMapping));
        }

        public void DeleteDeviceMapping(DeviceMapping deviceMapping)
        {
            if (deviceMapping == null)
                throw new ArgumentNullException("deviceMapping");

            if (!deviceMappings.Contains(deviceMapping)) return;
            deviceMappings.Remove(deviceMapping);

            var handler = DeviceMappingRemoved;
            if (handler != null)
                handler(this, new DeviceMappingEventArgs(deviceMapping));
        }

        /// <summary>
        /// Returns true if the specified deviceMapping exists in the
        /// repository, or false if it is not.
        /// </summary>
        public bool ContainsDeviceMapping(DeviceMapping deviceMapping)
        {
            if (deviceMapping == null)
                throw new ArgumentNullException("deviceMapping");

            return deviceMappings.Contains(deviceMapping);
        }

        /// <summary>
        /// Returns a shallow-copied list of all deviceMappings in the repository.
        /// </summary>
        public List<DeviceMapping> GetDeviceMappings()
        {
            return new List<DeviceMapping>(deviceMappings);
        }

        #endregion // Public Interface

        #region Private Helpers

        private static T LoadChanges<T>(string fileName)
        {
            object list = null;
            // Deserialization
            if (File.Exists(fileName))
            {
                try
                {
                    using (var reader = XmlReader.Create(fileName))
                    {
                        var serializer = new XmlSerializer(typeof(T));
                        if (serializer.CanDeserialize(reader))
                        {
                            list = serializer.Deserialize(reader);
                            //foreach (object item in (list as ISerializableList).Items)
                            //{
                            //    dataSource.Add(item);
                            //}
                        }
                        reader.Close();
                    }
                }
                catch (XmlException ex)
                {
                    System.Windows.Forms.MessageBox.Show(
                        string.Format(CultureInfo.InvariantCulture,
                            "Could not parse the file '{0}'.  Received the following error message:\n\r{1}",
                            fileName,
                            ex.Message));
                }
            }

            return (T)list;
        }

        private static void RecordChanges<T>(string fileName, IEnumerator enumerator)
        {
            var newList = Activator.CreateInstance(typeof(T));
            while (enumerator.MoveNext())
            {
                ((ISerializableList)newList).Add(enumerator.Current);
            }
            // Serialization
            using (TextWriter writer = new StreamWriter(fileName))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, newList);
                writer.Close();
            }
        }
        #endregion // Private Helpers
    }
}