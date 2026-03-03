using System;
using System.Text;

namespace ClassicUO.Network;

internal sealed class Packet : PacketBase
{
	private readonly byte[] _data;

	protected override byte this[int index]
	{
		get
		{
			if (index < 0 || index >= Length)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return _data[index];
		}
		set
		{
			if (index < 0 || index >= Length)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			_data[index] = value;
			IsChanged = true;
		}
	}

	public override int Length { get; }

	public bool IsChanged { get; private set; }

	public bool Filter { get; set; }

	public Packet(byte[] data, int length)
	{
		_data = data;
		Length = length;
	}

	public override byte[] ToArray()
	{
		return _data;
	}

	public void MoveToData()
	{
		Seek(3);
	}

	protected override void EnsureSize(int length)
	{
		if (length < 0 || base.Position + length > Length)
		{
			throw new ArgumentOutOfRangeException("length");
		}
	}

	public byte ReadByte()
	{
		EnsureSize(1);
		return _data[base.Position++];
	}

	public sbyte ReadSByte()
	{
		return (sbyte)ReadByte();
	}

	public bool ReadBool()
	{
		return ReadByte() != 0;
	}

	public ushort ReadUShort()
	{
		EnsureSize(2);
		return (ushort)((ReadByte() << 8) | ReadByte());
	}

	public uint ReadUInt()
	{
		EnsureSize(4);
		return (uint)((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
	}

	public string ReadASCII()
	{
		EnsureSize(1);
		StringBuilder stringBuilder = new StringBuilder();
		char value;
		while ((value = (char)ReadByte()) != 0)
		{
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	public string ReadASCII(int length, bool exitIfNull = false)
	{
		EnsureSize(length);
		StringBuilder stringBuilder = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			char c = (char)ReadByte();
			if (c != 0)
			{
				stringBuilder.Append(c);
			}
			else if (exitIfNull)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	public string ReadUnicode()
	{
		EnsureSize(2);
		StringBuilder stringBuilder = new StringBuilder();
		char value;
		while ((value = (char)ReadUShort()) != 0)
		{
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	public string ReadUnicode(int length)
	{
		EnsureSize(length);
		StringBuilder stringBuilder = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			char c = (char)ReadUShort();
			if (c != 0)
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public byte[] ReadArray(int count)
	{
		EnsureSize(count);
		byte[] array = new byte[count];
		Buffer.BlockCopy(_data, base.Position, array, 0, count);
		base.Position += count;
		return array;
	}

	public string ReadUnicodeReversed(int length)
	{
		EnsureSize(length);
		length /= 2;
		StringBuilder stringBuilder = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			char c = (char)ReadUShortReversed();
			if (c != 0)
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public string ReadUnicodeReversed()
	{
		EnsureSize(2);
		StringBuilder stringBuilder = new StringBuilder();
		char value;
		while ((value = (char)ReadUShortReversed()) != 0)
		{
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	public ushort ReadUShortReversed()
	{
		return (ushort)(ReadByte() | (ReadByte() << 8));
	}
}
