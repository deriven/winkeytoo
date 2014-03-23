using System;
using System.Collections.Generic;
using WinKeyToo.Instrumentation;
using WinKeyToo.Internals;

namespace WinKeyToo
{
    internal class MapSequence : IMapSequence
    {
        private readonly List<int> inputSequence;
        private readonly List<IMapActionPlugin> mapActions;

        public MapSequence()
        {
            inputSequence = new List<int>();
            mapActions = new List<IMapActionPlugin>();
        }

        public MapSequence(int startingInput) : this()
        {
            inputSequence.Add(startingInput);
        }

        #region IMapSequence Members

        public IMapSequence FollowedBy(int input)
        {
            inputSequence.Add(input);
            return this;
        }

        public void To(IMapActionPlugin actionPlugin)
        {
            mapActions.Add(actionPlugin);
        }

        public void Receive<T>(T[] inputCombination)
        {
            var matchCount = 0;
            foreach(var input in inputCombination)
            {
                var inputNumber = Convert.ToInt32(input);
                using (var tracing = new Tracing(false))
                {
                    tracing.WriteInfo("Received " + inputNumber);
                }
                if (inputNumber != 0 && inputSequence.Contains(inputNumber)) matchCount++;
            }
            if (matchCount != inputSequence.Count) return;
            foreach (var mapAction in mapActions) mapAction.Execute();
        }

        #endregion
    }
}
