using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool OnGetPlayerPosition(out int x, out int y, out int z);
