using System;

namespace RTSerialCom;

public class DataStreamEventArgs : EventArgs
{
	private byte[] _bytes;

	public byte[] Response => _bytes;

	public DataStreamEventArgs(byte[] bytes)
	{
		_bytes = bytes;
	}
}
