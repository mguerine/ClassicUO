using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool RequestMove(int dir, bool run);
