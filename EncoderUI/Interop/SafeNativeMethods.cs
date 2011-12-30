namespace EBrake.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public sealed class DEV_BROADCAST_HDR
    {
        public int dbcv_size;       // size of the struct
        public DBT_DEVTYP dbcv_devicetype; // DBT_DEVTYP_VOLUME
        public int dbcv_reserved;   // reserved; do not use
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class DEV_BROADCAST_VOLUME
    {
        public int dbcv_size;       // size of the struct
        public DBT_DEVTYP dbcv_devicetype; // DBT_DEVTYP_VOLUME
        public int dbcv_reserved;   // reserved; do not use
        public int dbcv_unitmask;   // Bit 0=A, bit 1=B, and so on (bitmask)
        public DBTF dbcv_flags;    // DBTF_MEDIA=0x01, DBTF_NET=0x02 (bitmask)
    }

    public enum WM
    {
        DEVICECHANGE = 0x219
    }

    public enum DBT
    {
        DEVICEARRIVAL = 0x8000,
        DEVICEREMOVECOMPLETE = 0x8004,
    }

    public enum DBT_DEVTYP : int
    {
        VOLUME = 0x00000002,
    }

    [Flags]
    public enum DBTF : short
    {
        MEDIA = 0x0001,
    }

    static class SafeNativeMethods
    {

    }
}
