using ClassicUO.Network;

namespace CUO_APINetPipes;

internal class PCastSpell : PacketWriter
{
	public PCastSpell(uint idx)
		: base(11)
	{
		WriteUInt(idx);
	}
}
