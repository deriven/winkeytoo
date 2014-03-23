using WinKeyToo.Internals;

namespace WinKeyToo
{
    internal interface IMapSequence
    {
        IMapSequence FollowedBy(int input);
        void To(IMapActionPlugin actionPlugin);
        void Receive<T>(T[] inputCombination);
    }
}