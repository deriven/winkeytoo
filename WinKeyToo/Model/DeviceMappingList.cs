using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace WinKeyToo.Model
{
    [XmlRoot("Mappings")]
    [XmlInclude(typeof(DeviceMapping))]
    public class DeviceMappingList : ObservableCollection<DeviceMapping>, ISerializableList
    {
        [XmlElement("Mapping")]
        public object[] SerializableItems
        {
            get
            {
                var events = new DeviceMapping[Count];
                CopyTo(events, 0);
                return events;
            }
            set
            {
                if (value == null) return;
                Clear();
                foreach (var item in value)
                    Add((item as DeviceMapping));
            }
        }

        public void Add(object item)
        {
            base.Add(item as DeviceMapping);
        }
    }
}
