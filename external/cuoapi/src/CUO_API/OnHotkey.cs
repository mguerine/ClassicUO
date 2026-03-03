using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool OnHotkey(int key, int mod, bool pressed);
