using System;

namespace ClassicUO.Network;

internal abstract class PacketBase
{
	protected abstract byte this[int index] { get; set; }

	public abstract int Length { get; }

	public byte ID => this[0];

	public int Position { get; protected set; }

	protected abstract void EnsureSize(int length);

	public abstract byte[] ToArray();

	public void Skip(int lengh)
	{
		EnsureSize(lengh);
		Position += lengh;
	}

	public void Seek(int index)
	{
		Position = index;
		EnsureSize(0);
	}

	public void WriteByte(byte v)
	{
		EnsureSize(1);
		this[Position++] = v;
	}

	public void WriteBytes(byte[] buffer, int v, int length)
	{
		EnsureSize(length);
		for (int i = v; i < length; i++)
		{
			this[Position++] = buffer[i];
		}
	}

	public void WriteSByte(sbyte v)
	{
		WriteByte((byte)v);
	}

	public void WriteBool(bool v)
	{
		WriteByte((byte)(v ? 1 : 0));
	}

	public void WriteUShort(ushort v)
	{
		EnsureSize(2);
		WriteByte((byte)(v >> 8));
		WriteByte((byte)v);
	}

	public void WriteUInt(uint v)
	{
		EnsureSize(4);
		WriteByte((byte)(v >> 24));
		WriteByte((byte)(v >> 16));
		WriteByte((byte)(v >> 8));
		WriteByte((byte)v);
	}

	public unsafe void WriteASCII(string value)
	{
		EnsureSize(value.Length + 1);
		fixed (char* ptr = value)
		{
			char* ptr2 = ptr;
			while (*ptr2 != 0)
			{
				WriteByte((byte)(*(ptr2++)));
			}
		}
		WriteByte(0);
	}

	public unsafe void WriteASCII(string value, int length)
	{
		EnsureSize(length);
		if (value.Length > length)
		{
			throw new ArgumentOutOfRangeException();
		}
		fixed (char* ptr = value)
		{
			char* ptr2 = ptr;
			byte* ptr3 = (byte*)ptr + length;
			while (*ptr2 != 0 && &ptr2 != &ptr3)
			{
				WriteByte((byte)(*(ptr2++)));
			}
		}
		if (value.Length < length)
		{
			WriteByte(0);
			Position += length - value.Length - 1;
		}
	}

	public unsafe void WriteUnicode(string value)
	{
		EnsureSize((value.Length + 1) * 2);
		fixed (char* ptr = value)
		{
			short* ptr2 = (short*)ptr;
			while (*ptr2 != 0)
			{
				WriteUShort((ushort)(*(ptr2++)));
			}
		}
		WriteUShort(0);
	}

	public unsafe void WriteUnicode(string value, int length)
	{
		EnsureSize(length);
		if (value.Length > length)
		{
			throw new ArgumentOutOfRangeException();
		}
		fixed (char* ptr = value)
		{
			short* ptr2 = (short*)ptr;
			while (*ptr2 != 0)
			{
				WriteUShort((ushort)(*(ptr2++)));
			}
		}
		if (value.Length < length)
		{
			WriteUShort(0);
			Position += (length - value.Length - 1) * 2;
		}
	}
}
