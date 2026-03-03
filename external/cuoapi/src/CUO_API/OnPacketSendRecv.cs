using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool OnPacketSendRecv(ref byte[] data, ref int length);
