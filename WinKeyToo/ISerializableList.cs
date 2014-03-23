namespace WinKeyToo
{
    internal interface ISerializableList
    {
        object[] SerializableItems { get; set; }
        void Add(object item);
    }
}
