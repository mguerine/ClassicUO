using System;
using System.Collections.Generic;

namespace CUO_APINetPipes;

internal class SendQueue
{
	public class Gram
	{
		private static readonly Stack<Gram> _Pool = new Stack<Gram>();

		public byte[] Buffer { get; private set; }

		public int Length { get; private set; }

		public int Available => Buffer.Length - Length;

		public bool IsFull => Length == Buffer.Length;

		private Gram()
		{
		}

		public static Gram Acquire()
		{
			lock (_Pool)
			{
				Gram gram = ((_Pool.Count <= 0) ? new Gram() : _Pool.Pop());
				gram.Buffer = AcquireBuffer();
				gram.Length = 0;
				return gram;
			}
		}

		public int Write(byte[] buffer, int offset, int length)
		{
			int num = Math.Min(length, Available);
			System.Buffer.BlockCopy(buffer, offset, Buffer, Length, num);
			Length += num;
			return num;
		}

		public void Release()
		{
			lock (_Pool)
			{
				_Pool.Push(this);
				ReleaseBuffer(Buffer);
			}
		}
	}

	private const int PendingCap = 262144;

	private static readonly int _CoalesceBufferSize = 512;

	private static readonly BufferPool _UnusedBuffers = new BufferPool(2048, _CoalesceBufferSize);

	private readonly Queue<Gram> _pending;

	private Gram _buffered;

	public bool IsFlushReady
	{
		get
		{
			if (_pending.Count == 0)
			{
				return _buffered != null;
			}
			return false;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (_pending.Count == 0)
			{
				return _buffered == null;
			}
			return false;
		}
	}

	public SendQueue()
	{
		_pending = new Queue<Gram>();
	}

	public static byte[] AcquireBuffer()
	{
		lock (_UnusedBuffers)
		{
			return _UnusedBuffers.GetFreeSegment();
		}
	}

	public static void ReleaseBuffer(byte[] buffer)
	{
		lock (_UnusedBuffers)
		{
			if (buffer != null && buffer.Length == _CoalesceBufferSize)
			{
				_UnusedBuffers.AddFreeSegment(buffer);
			}
		}
	}

	public Gram CheckFlushReady()
	{
		Gram buffered = _buffered;
		_pending.Enqueue(_buffered);
		_buffered = null;
		return buffered;
	}

	public Gram Dequeue()
	{
		Gram result = null;
		if (_pending.Count > 0)
		{
			_pending.Dequeue().Release();
			if (_pending.Count > 0)
			{
				result = _pending.Peek();
			}
		}
		return result;
	}

	public Gram Enqueue(byte[] buffer, int length)
	{
		return Enqueue(buffer, 0, length);
	}

	public Gram Enqueue(byte[] buffer, int offset, int length)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0 || offset >= buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset", offset, "Offset must be greater than or equal to zero and less than the size of the buffer.");
		}
		if (length < 0 || length > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("length", length, "Length cannot be less than zero or greater than the size of the buffer.");
		}
		if (buffer.Length - offset < length)
		{
			throw new ArgumentException("Offset and length do not point to a valid segment within the buffer.");
		}
		if (_pending.Count * _CoalesceBufferSize + (_buffered?.Length ?? 0) + length > 262144)
		{
			throw new CapacityExceededException();
		}
		Gram result = null;
		while (length > 0)
		{
			if (_buffered == null)
			{
				_buffered = Gram.Acquire();
			}
			int num = _buffered.Write(buffer, offset, length);
			offset += num;
			length -= num;
			if (_buffered.IsFull)
			{
				if (_pending.Count == 0)
				{
					result = _buffered;
				}
				_pending.Enqueue(_buffered);
				_buffered = null;
			}
		}
		return result;
	}

	public void Clear()
	{
		if (_buffered != null)
		{
			_buffered.Release();
			_buffered = null;
		}
		while (_pending.Count > 0)
		{
			_pending.Dequeue().Release();
		}
	}
}
