using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void OnUpdatePlayerPosition(int x, int y, int z);
