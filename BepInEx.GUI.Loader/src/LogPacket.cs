using LogEventArgs = BepInEx.Logging.LogEventArgs;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace BepInEx.GUI.Loader;

internal unsafe struct LogPacket
{
    internal byte[] Bytes;
    //  _____________________________________
    // |Field                        | Offset|
    // |Log String Byte Array Length | 0x0000|
    // |Log Level                    | 0x0004|
    // |Log String Byte Array        | 0x0008|

    internal unsafe LogPacket(LogEventArgs log, string logString)
    {
        byte[] logStringByteArray = Encoding.UTF8.GetBytes(logString ?? $"{log}");

        int payloadSize = logStringByteArray.Length;

        const Int32 SizeOfLengthPrefix = sizeof(UInt32);
        const Int32 SizeOfLogLevel = sizeof(Int32);

        Bytes = new byte[SizeOfLengthPrefix + SizeOfLogLevel + payloadSize];

        fixed (byte* byteArrayPtr = Bytes)
        {
            *(UInt32*)byteArrayPtr = (UInt32)payloadSize;

            *(Int32*)(&byteArrayPtr[SizeOfLengthPrefix]) = (Int32)log.Level;

            Marshal.Copy(logStringByteArray, 0, (IntPtr)(&byteArrayPtr[SizeOfLengthPrefix + SizeOfLogLevel]), payloadSize);
        }
    }
    internal unsafe LogPacket(LogEventArgs log)
    {
        byte[] logStringByteArray = Encoding.UTF8.GetBytes($"{log}");

        int payloadSize = logStringByteArray.Length;

        const Int32 SizeOfLengthPrefix = sizeof(UInt32);
        const Int32 SizeOfLogLevel = sizeof(Int32);

        Bytes = new byte[SizeOfLengthPrefix + SizeOfLogLevel + payloadSize];

        fixed (byte* byteArrayPtr = Bytes)
        {
            *(UInt32*)byteArrayPtr = (UInt32)payloadSize;

            *(Int32*)(&byteArrayPtr[SizeOfLengthPrefix]) = (Int32)log.Level;

            Marshal.Copy(logStringByteArray, 0, (IntPtr)(&byteArrayPtr[SizeOfLengthPrefix + SizeOfLogLevel]), payloadSize);
        }
    }
}
