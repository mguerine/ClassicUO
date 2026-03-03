using ClassicUO.Network;

namespace CUO_APINetPipes;

internal class PSpecialClassicUOQuery : PacketWriter
{
	public PSpecialClassicUOQuery(SpecialQuery query)
		: base(13)
	{
		WriteByte((byte)query);
	}
}
