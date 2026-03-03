using ClassicUO.Network;

namespace CUO_APINetPipes;

internal class PAttack : PacketWriter
{
	public PAttack(uint serial)
		: base(12)
	{
		WriteUInt(serial);
	}
}
