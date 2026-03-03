using System.Runtime.InteropServices;

namespace CUO_API;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void OnGetStaticImage(ushort g, ref ArtInfo art);
