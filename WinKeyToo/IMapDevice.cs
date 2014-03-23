namespace WinKeyToo
{
    internal interface IMapDevice
    {
        void Start();
        void Stop();
        IMapSequence StartNewSequence { get; }
        IMapSequence Map(int input);
    }
}