using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SensorsData100HZ
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
	public short[] acc;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
	public short[] mag;

	public float longitude;

	public float latitude;

	public float speed;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
	public float[] rotation;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
	public float[] gravity;

	public uint activation;
}
