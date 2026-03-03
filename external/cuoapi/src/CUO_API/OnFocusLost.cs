using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void OnFocusLost();
