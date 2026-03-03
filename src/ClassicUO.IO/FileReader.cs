using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    public abstract class FileReader : IDisposable
    {
        private long _position;
        private readonly FileStream _stream;

        protected FileReader(FileStream stream)
        {
            _stream = stream;
        }

        public string FilePath => _stream.Name;
        public long Length => _stream.Length;
        public long Position => _position;

        public abstract BinaryReader Reader { get; }

        public virtual void Dispose()
        {
            Reader?.Dispose();
            _stream?.Dispose();
        }

        public void Seek(long index, SeekOrigin origin) => _position = Reader.BaseStream.Seek(index, origin);
        public sbyte ReadInt8() { _position += sizeof(sbyte); return Reader.ReadSByte(); }
        public byte ReadUInt8() { _position += sizeof(byte); return Reader.ReadByte(); }
        public short ReadInt16() { _position += sizeof(short); return Reader.ReadInt16(); }
        public ushort ReadUInt16() { _position += sizeof(ushort); return Reader.ReadUInt16(); }
        public int ReadInt32() { _position += sizeof(int); return Reader.ReadInt32(); }
        public uint ReadUInt32() { _position += sizeof(uint); return Reader.ReadUInt32(); }
        public long ReadInt64() { _position += sizeof(long); return Reader.ReadInt64(); }
        public ulong ReadUInt64() { _position += sizeof(ulong); return Reader.ReadUInt64(); }
        public int Read(Span<byte> buffer)
        {
            _position += buffer.Length;
            if (buffer.Length == 0) return 0;
            var temp = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int read = Reader.BaseStream.Read(temp, 0, buffer.Length);
                new ReadOnlySpan<byte>(temp, 0, read).CopyTo(buffer);
                return read;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(temp);
            }
        }
        public unsafe T Read<T>() where T : unmanaged
        {
            Unsafe.SkipInit<T>(out var v);
            var p = new Span<byte>(&v, sizeof(T));
            Read(p);
            return v;
        }
    }
}
