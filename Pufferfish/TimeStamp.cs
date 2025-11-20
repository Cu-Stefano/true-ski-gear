using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TimeStamp
{
	public uint msec;
}
