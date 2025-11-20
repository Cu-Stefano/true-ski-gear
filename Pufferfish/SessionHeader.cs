using System.Runtime.InteropServices;

namespace PufferFish;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SessionHeader
{
	public DataHeader header;

	public uint id;

	public uint board_id;

	public uint fw_version;

	public byte day;

	public byte month;

	public byte year;

	public ushort acc_full_scale;

	public ushort acc_rate;

	public ushort ext_acc_full_scale;

	public ushort ext_acc_rate;

	public byte ext_status;

	public ushort gyro_full_scale;

	public ushort gyro_rate;

	public ushort mag_full_scale;

	public ushort mag_rate;

	public ushort gps_rate;

	public uint activations;
}
