using System;
using System.IO.Ports;
using System.Threading;

namespace RTSerialCom;

public class SerialClient : IDisposable
{
	private string _port;

	private int _baudRate;

	private SerialPort _serialPort;

	private Thread serThread;

	private double _PacketsRate;

	private DateTime _lastReceive;

	private const int freqCriticalLimit = 20;

	public string Port => _port;

	public int BaudRate => _baudRate;

	public string ConnectionString => $"[Serial] Port: {_serialPort.PortName} | Baudrate: {_serialPort.BaudRate.ToString()}";

	public event EventHandler<DataStreamEventArgs> OnReceiving;

	public SerialClient(string port)
	{
		_port = port;
		_baudRate = 9600;
		_lastReceive = DateTime.MinValue;
	}

	public SerialClient(string Port, int baudRate)
		: this(Port)
	{
		_baudRate = baudRate;
	}

	public bool OpenConn()
	{
		try
		{
			if (_serialPort == null)
			{
				_serialPort = new SerialPort(_port, _baudRate, Parity.None);
			}
			if (!_serialPort.IsOpen)
			{
				_serialPort.ReadTimeout = 2000;
				_serialPort.WriteTimeout = 500;
				_serialPort.Open();
				if (_serialPort.IsOpen)
				{
					serThread = new Thread(SerialReceiving);
					serThread.Priority = ThreadPriority.Normal;
					serThread.Name = "SerialHandle" + serThread.ManagedThreadId;
					serThread.Start();
				}
			}
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public bool OpenConn(string port, int baudRate)
	{
		_port = port;
		_baudRate = baudRate;
		return OpenConn();
	}

	public void CloseConn()
	{
		if (_serialPort != null && _serialPort.IsOpen)
		{
			serThread.Abort();
			if (serThread.ThreadState == ThreadState.Aborted)
			{
				_serialPort.Close();
			}
		}
	}

	public bool ResetConn()
	{
		CloseConn();
		return OpenConn();
	}

	public void Transmit(byte[] packet)
	{
		_serialPort.Write(packet, 0, packet.Length);
	}

	public int Receive(byte[] bytes, int offset, int count)
	{
		int readBytes = 0;
		if (count > 0)
		{
			readBytes = _serialPort.Read(bytes, offset, count);
		}
		return readBytes;
	}

	public void Dispose()
	{
		CloseConn();
		if (_serialPort != null)
		{
			_serialPort.Dispose();
			_serialPort = null;
		}
	}

	private void SerialReceiving()
	{
		while (_serialPort != null && _serialPort.IsOpen)
		{
			try
			{
				int count = _serialPort.BytesToRead;
				TimeSpan tmpInterval = DateTime.Now - _lastReceive;
				byte[] buf = new byte[count];
				int readBytes = Receive(buf, 0, count);
				if (readBytes > 0)
				{
					OnSerialReceiving(buf);
				}
				_PacketsRate = (_PacketsRate + (double)readBytes) / 2.0;
				_lastReceive = DateTime.Now;
				if ((double)(readBytes + _serialPort.BytesToRead) / 2.0 <= _PacketsRate && tmpInterval.Milliseconds > 0)
				{
					Thread.Sleep((tmpInterval.Milliseconds > 20) ? 20 : tmpInterval.Milliseconds);
				}
			}
			catch
			{
			}
		}
	}

	private void OnSerialReceiving(byte[] res)
	{
		if (this.OnReceiving != null)
		{
			this.OnReceiving(this, new DataStreamEventArgs(res));
		}
	}

	internal void DiscardInBuffer()
	{
		if (_serialPort != null && _serialPort.IsOpen)
		{
			_serialPort.DiscardInBuffer();
		}
	}

	internal bool IsOpen()
	{
		if (_serialPort != null && _serialPort.IsOpen)
		{
			return _serialPort.IsOpen;
		}
		return false;
	}
}
