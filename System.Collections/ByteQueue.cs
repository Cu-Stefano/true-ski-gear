namespace System.Collections;

public sealed class ByteQueue
{
	private int fHead;

	private int fTail;

	private int fSize;

	private int fSizeUntilCut;

	private byte[] fInternalBuffer;

	public int Length => fSize;

	public ByteQueue()
	{
		fInternalBuffer = new byte[4096];
	}

	internal void Clear()
	{
		fHead = 0;
		fTail = 0;
		fSize = 0;
		fSizeUntilCut = fInternalBuffer.Length;
	}

	internal void Clear(int size)
	{
		lock (this)
		{
			if (size > fSize)
			{
				size = fSize;
			}
			if (size != 0)
			{
				fHead = (fHead + size) % fInternalBuffer.Length;
				fSize -= size;
				if (fSize == 0)
				{
					fHead = 0;
					fTail = 0;
				}
				fSizeUntilCut = fInternalBuffer.Length - fHead;
			}
		}
	}

	private void SetCapacity(int capacity)
	{
		byte[] newBuffer = new byte[capacity];
		if (fSize > 0)
		{
			if (fHead < fTail)
			{
				Buffer.BlockCopy(fInternalBuffer, fHead, newBuffer, 0, fSize);
			}
			else
			{
				Buffer.BlockCopy(fInternalBuffer, fHead, newBuffer, 0, fInternalBuffer.Length - fHead);
				Buffer.BlockCopy(fInternalBuffer, 0, newBuffer, fInternalBuffer.Length - fHead, fTail);
			}
		}
		fHead = 0;
		fTail = fSize;
		fInternalBuffer = newBuffer;
	}

	internal void Enqueue(byte[] buffer, int offset, int size)
	{
		if (size == 0)
		{
			return;
		}
		lock (this)
		{
			if (fSize + size > fInternalBuffer.Length)
			{
				SetCapacity((fSize + size + 2047) & -2048);
			}
			if (fHead < fTail)
			{
				int rightLength = fInternalBuffer.Length - fTail;
				if (rightLength >= size)
				{
					Buffer.BlockCopy(buffer, offset, fInternalBuffer, fTail, size);
				}
				else
				{
					Buffer.BlockCopy(buffer, offset, fInternalBuffer, fTail, rightLength);
					Buffer.BlockCopy(buffer, offset + rightLength, fInternalBuffer, 0, size - rightLength);
				}
			}
			else
			{
				Buffer.BlockCopy(buffer, offset, fInternalBuffer, fTail, size);
			}
			fTail = (fTail + size) % fInternalBuffer.Length;
			fSize += size;
			fSizeUntilCut = fInternalBuffer.Length - fHead;
		}
	}

	internal byte[] Dequeue(int offset, int size)
	{
		byte[] ret = new byte[size];
		Dequeue(ret, offset, size);
		return ret;
	}

	internal int Dequeue(byte[] buffer, int offset, int size)
	{
		lock (this)
		{
			if (size > fSize)
			{
				size = fSize;
			}
			if (size == 0)
			{
				return 0;
			}
			if (fHead < fTail)
			{
				Buffer.BlockCopy(fInternalBuffer, fHead, buffer, offset, size);
			}
			else
			{
				int rightLength = fInternalBuffer.Length - fHead;
				if (rightLength >= size)
				{
					Buffer.BlockCopy(fInternalBuffer, fHead, buffer, offset, size);
				}
				else
				{
					Buffer.BlockCopy(fInternalBuffer, fHead, buffer, offset, rightLength);
					Buffer.BlockCopy(fInternalBuffer, 0, buffer, offset + rightLength, size - rightLength);
				}
			}
			fHead = (fHead + size) % fInternalBuffer.Length;
			fSize -= size;
			if (fSize == 0)
			{
				fHead = 0;
				fTail = 0;
			}
			fSizeUntilCut = fInternalBuffer.Length - fHead;
			return size;
		}
	}

	private byte PeekOne(int index)
	{
		return (index >= fSizeUntilCut) ? fInternalBuffer[index - fSizeUntilCut] : fInternalBuffer[fHead + index];
	}

	public byte At(int index)
	{
		return PeekOne(index);
	}
}
