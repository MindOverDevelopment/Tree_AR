using System;
using System.Threading;

namespace Broccoli.Utils
{
    public class Debouncer
    {
        private readonly int delayMilliseconds;
        private Timer debounceTimer;
        private SynchronizationContext context;

        public Debouncer(int delayMilliseconds)
        {
            this.delayMilliseconds = delayMilliseconds;
            context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public void Debounce(Action<object[]> action, params object[] parameters)
        {
            debounceTimer?.Dispose();
            debounceTimer = new Timer(_ => context.Post(_ => action(parameters), null), null, delayMilliseconds, Timeout.Infinite);
        }
    }
}