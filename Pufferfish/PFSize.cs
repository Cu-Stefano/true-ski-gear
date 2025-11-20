using System;

namespace PufferFish;

internal class PFSize
{
	private const ulong TH_NAND_PAGES_IN_BLOCK = 64uL;

	private const ulong TH_NAND_BLOCKS_IN_CHIP = 8192uL;

	private const ulong TH_NAND_COLS_IN_PAGE = 4328uL;

	private const ulong TH_NAND_BYTES_IN_CHIP = 2269118464uL;

	private const ulong TH_NAND_BYTES_IN_BLOCK = 276992uL;

	private const ulong TH_NAND_BYTES_IN_PAGE = 4328uL;

	private ulong actualSize;

	public PFSize(ulong value)
	{
		actualSize = value;
	}

	public PFSize(byte[] buffer, int offset)
	{
		uint _size = BitConverter.ToUInt32(buffer, offset);
		byte _chips = buffer[offset + 4];
		actualSize = (ulong)(_size + (long)_chips * 2269118464L);
	}

	internal int WriteToBuffer(byte[] buffer, int offset)
	{
		uint size = (uint)(actualSize % 2269118464u);
		byte chip = (byte)(actualSize / 2269118464u);
		BitConverter.GetBytes(size).CopyTo(buffer, offset);
		buffer[offset + 4] = chip;
		return 5;
	}

	public static implicit operator PFSize(ulong value)
	{
		return new PFSize(value);
	}

	internal ulong getSize()
	{
		return actualSize;
	}
}
