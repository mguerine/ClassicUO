using System;

namespace CUO_API.NetPipes;

internal sealed class CircularBuffer
{
	private byte[] _buffer;

	private int _head;

	private int _tail;

	public int Length { get; private set; }

	public CircularBuffer()
	{
		_buffer = new byte[65536];
	}

	internal void Clear()
	{
		_head = 0;
		_tail = 0;
		Length = 0;
	}

	private void SetCapacity(int capacity)
	{
		byte[] array = new byte[capacity];
		if (Length > 0)
		{
			if (_head < _tail)
			{
				Buffer.BlockCopy(_buffer, _head, array, 0, Length);
			}
			else
			{
				Buffer.BlockCopy(_buffer, _head, array, 0, _buffer.Length - _head);
				Buffer.BlockCopy(_buffer, 0, array, _buffer.Length - _head, _tail);
			}
		}
		_head = 0;
		_tail = Length;
		_buffer = array;
	}

	internal void Enqueue(byte[] buffer, int offset, int size)
	{
		if (Length + size > _buffer.Length)
		{
			SetCapacity((Length + size + 2047) & -2048);
		}
		if (_head < _tail)
		{
			int num = _buffer.Length - _tail;
			if (num >= size)
			{
				Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
			}
			else
			{
				Buffer.BlockCopy(buffer, offset, _buffer, _tail, num);
				Buffer.BlockCopy(buffer, offset + num, _buffer, 0, size - num);
			}
		}
		else
		{
			Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
		}
		_tail = (_tail + size) % _buffer.Length;
		Length += size;
	}

	internal int Dequeue(byte[] buffer, int offset, int size)
	{
		if (size > Length)
		{
			size = Length;
		}
		if (size == 0)
		{
			return 0;
		}
		if (_head < _tail)
		{
			Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
		}
		else
		{
			int num = _buffer.Length - _head;
			if (num >= size)
			{
				Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
			}
			else
			{
				Buffer.BlockCopy(_buffer, _head, buffer, offset, num);
				Buffer.BlockCopy(_buffer, 0, buffer, offset + num, size - num);
			}
		}
		_head = (_head + size) % _buffer.Length;
		Length -= size;
		if (Length == 0)
		{
			_head = 0;
			_tail = 0;
		}
		return size;
	}

	public byte GetID()
	{
		if (Length >= 1)
		{
			return _buffer[_head];
		}
		return byte.MaxValue;
	}

	public int GetLength()
	{
		if (Length >= 3)
		{
			return _buffer[(_head + 2) % _buffer.Length] | (_buffer[(_head + 1) % _buffer.Length] << 8);
		}
		return 0;
	}
}
