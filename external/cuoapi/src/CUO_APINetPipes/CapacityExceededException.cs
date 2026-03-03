using System;

namespace CUO_APINetPipes;

[Serializable]
public sealed class CapacityExceededException : Exception
{
	public CapacityExceededException()
		: base("Too much data pending.")
	{
	}
}
