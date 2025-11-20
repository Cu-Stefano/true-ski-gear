using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DataHeader
{
	public byte type;

	public byte size;
}
