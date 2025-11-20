using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal class SensorsData100HZStruct
{
	public DataHeader header;

	public TimeStamp t;

	public SensorsData100HZ data;
}
