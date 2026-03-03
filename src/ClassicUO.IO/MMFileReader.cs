using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace ClassicUO.IO
{
    public class MMFileReader : FileReader
    {
        private MemoryMappedViewAccessor _accessor;
        private MemoryMappedFile _mmf;
        private BinaryReader _file;

        public MMFileReader(FileStream stream) : base(stream)
        {
            if (Length <= 0)
                return;

            if (IntPtr.Size == 4)
            {
                _file = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                return;
            }

            try
            {
                _mmf = MemoryMappedFile.CreateFromFile
                (
                    stream,
                    null,
                    0,
                    MemoryMappedFileAccess.Read,
                    HandleInheritability.None,
                    false
                );

                _accessor = _mmf.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);

                unsafe
                {
                    byte* ptr = null;
                    _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    _file = new BinaryReader(new UnmanagedMemoryStream(ptr, Length));
                }
            }
            catch (IOException)
            {
                _accessor?.Dispose();
                _mmf?.Dispose();
                _accessor = null;
                _mmf = null;
                stream.Seek(0, SeekOrigin.Begin);
                _file = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            }
        }

        public override BinaryReader Reader => _file;

        public override void Dispose()
        {
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _mmf?.Dispose();
            _accessor = null;
            _mmf = null;

            base.Dispose();
        }
    }
}
