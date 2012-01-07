namespace EBrake
{
    using System;
    using System.Windows.Threading;

    static class DispatcherExtensions
    {
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action a)
        {
            return dispatcher.BeginInvoke(a, null);
        }

        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action a, DispatcherPriority priority)
        {
            return dispatcher.BeginInvoke(a, priority, null);
        }

        public static DispatcherOperation BeginInvoke<T1, T2>(this Dispatcher dispatcher, Action<T1, T2> a, T1 p1, T2 p2)
        {
            return dispatcher.BeginInvoke(a, new object[] { p1, p2 });
        }
    }
}
