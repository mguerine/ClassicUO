using ClassicUO.Network;

namespace CUO_APINetPipes;

internal class PSendPacketData : PacketWriter
{
	public PSendPacketData(bool toClassicUO, in byte[] data, int length, bool isdynamic)
		: base(10)
	{
		WriteBool(toClassicUO);
		WriteBool(isdynamic);
		WriteUShort((ushort)length);
		WriteBytes(data, 0, length);
	}
}
