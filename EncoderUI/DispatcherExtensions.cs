namespace EncoderUI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Threading;

    static class DispatcherExtensions
    {
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action a)
        {
            return dispatcher.BeginInvoke(a, null);
        }
    }
}
