using System;

namespace ClassicUO.Network;

internal class PacketWriter : PacketBase
{
	private byte[] _data;

	protected override byte this[int index]
	{
		get
		{
			return _data[index];
		}
		set
		{
			if (index == 0)
			{
				SetPacketId(value);
			}
			else
			{
				_data[index] = value;
			}
		}
	}

	public override int Length => _data.Length;

	public PacketWriter(byte id)
	{
		this[0] = id;
	}

	private void SetPacketId(byte id)
	{
		_data = new byte[32];
		_data[0] = id;
		base.Position = 3;
	}

	public override byte[] ToArray()
	{
		if (Length != base.Position)
		{
			Array.Resize(ref _data, base.Position);
		}
		WriteSize();
		return _data;
	}

	public void WriteSize()
	{
		this[1] = (byte)(base.Position >> 8);
		this[2] = (byte)base.Position;
	}

	protected override void EnsureSize(int length)
	{
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length");
		}
		while (base.Position + length > Length)
		{
			Array.Resize(ref _data, Length + length * 2);
		}
	}
}
