using System;

namespace PufferFish;

internal class Address
{
	private ushort column;

	private byte page;

	private ushort block;

	private byte chip;

	public Address(byte[] buffer, int offset)
	{
		column = BitConverter.ToUInt16(buffer, offset);
		page = buffer[offset + 2];
		block = BitConverter.ToUInt16(buffer, offset + 3);
		chip = buffer[offset + 5];
	}

	public Address()
	{
		column = 0;
		page = 0;
		block = 0;
		chip = 0;
	}

	public override string ToString()
	{
		return $"CO: 0x{column:X4} P: 0x{page:X4} B: 0x{block:X4} CH: 0x{chip:X2}";
	}
}
