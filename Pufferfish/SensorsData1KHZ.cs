using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SensorsData1KHZ
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
	public short[] gyro;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
	public short[] acc;

	public uint activation;
}
