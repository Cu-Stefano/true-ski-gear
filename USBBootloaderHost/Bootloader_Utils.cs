using System;
using System.Runtime.InteropServices;

namespace USBBootloaderHost;

internal class Bootloader_Utils
{
	public struct CyBtldr_CommunicationsData
	{
		public OpenConnection_USB OpenConnection;

		public CloseConnection_USB CloseConnection;

		public ReadData_USB ReadData;

		public WriteData_USB WriteData;

		public uint MaxTransferSize;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int OpenConnection_USB();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int CloseConnection_USB();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int ReadData_USB(IntPtr buffer, int size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int WriteData_USB(IntPtr buffer, int size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void CyBtldr_ProgressUpdate(byte arrayID, ushort rowNum);

	[DllImport("BootLoader_Utils.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern int CyBtldr_Program(string file, byte[] securityKey, byte appId, ref CyBtldr_CommunicationsData comm, CyBtldr_ProgressUpdate update);
}
