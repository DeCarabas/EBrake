namespace EBrake.Controls
{
    using System;

    public class ClosingWindowEventArgs : EventArgs
    {
        public bool Cancelled { get; set; }
    }
}
