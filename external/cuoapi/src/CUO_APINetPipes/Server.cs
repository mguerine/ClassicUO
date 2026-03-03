using System;
using System.Collections.Generic;
using System.IO.Pipes;
using CUO_API.NetPipes;
using ClassicUO.Network;

namespace CUO_APINetPipes;

public sealed class Server
{
	private const int BUFFER_SIZE = 131072;

	private NamedPipeServerStream _pipeStream;

	private byte[] _buffer;

	private CircularBuffer _byteQueue;

	private Queue<Packet> _workingQueue;

	private Queue<Packet> _queue;

	private readonly object _sync = new object();

	public bool IsDisposed { get; private set; }

	public bool IsConnected
	{
		get
		{
			if (_pipeStream != null)
			{
				return _pipeStream.IsConnected;
			}
			return false;
		}
	}

	public Server()
	{
		_buffer = new byte[131072];
		_workingQueue = new Queue<Packet>();
		_queue = new Queue<Packet>();
		_byteQueue = new CircularBuffer();
		_pipeStream = new NamedPipeServerStream("server", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
		_pipeStream.BeginWaitForConnection(OnConnect, null);
	}

	private void OnConnect(IAsyncResult result)
	{
		_pipeStream.EndWaitForConnection(result);
		_pipeStream.BeginRead(_buffer, 0, 131072, OnReceive, null);
	}

	private void OnReceive(IAsyncResult result)
	{
		int num = _pipeStream.EndRead(result);
		if (num > 0)
		{
			byte[] buffer = _buffer;
			lock (_byteQueue)
			{
				_byteQueue.Enqueue(buffer, 0, num);
			}
			ExtactPackets();
			if (_pipeStream.IsConnected)
			{
				_pipeStream.BeginRead(_buffer, 0, 131072, OnReceive, null);
			}
		}
		else
		{
			Disconnect();
		}
	}

	internal void Send(PacketWriter packet)
	{
		byte[] array = packet.ToArray();
		_pipeStream.Write(array, 0, array.Length);
	}

	public void Flush()
	{
		lock (_sync)
		{
			Queue<Packet> workingQueue = _workingQueue;
			_workingQueue = _queue;
			_queue = workingQueue;
		}
		while (_queue.Count > 0)
		{
			_queue.Dequeue();
		}
	}

	public void Disconnect()
	{
		if (IsDisposed)
		{
			return;
		}
		IsDisposed = true;
		if (_pipeStream == null)
		{
			return;
		}
		Flush();
		try
		{
			if (_pipeStream.IsConnected)
			{
				_pipeStream.Disconnect();
			}
			_pipeStream.Dispose();
		}
		catch (Exception)
		{
		}
		_byteQueue = null;
		_pipeStream = null;
		_buffer = null;
	}

	private void ExtactPackets()
	{
		if (!_pipeStream.IsConnected || _byteQueue == null || _byteQueue.Length <= 0)
		{
			return;
		}
		lock (_byteQueue)
		{
			int length = _byteQueue.Length;
			while (length > 0 && _pipeStream.IsConnected)
			{
				_byteQueue.GetID();
				int length2 = _byteQueue.GetLength();
				if (length < length2)
				{
					break;
				}
				byte[] array = new byte[length2];
				length2 = _byteQueue.Dequeue(array, 0, length2);
				lock (_sync)
				{
					Packet item = new Packet(array, length2);
					_workingQueue.Enqueue(item);
				}
				length = _byteQueue.Length;
			}
		}
	}
}
