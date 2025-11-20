using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SensorsData1KHZStruct
{
	public DataHeader header;

	public TimeStamp t;

	public SensorsData1KHZ data;
}
