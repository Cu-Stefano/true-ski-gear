using System;

namespace PufferFish;

public class SensorData
{
	public uint index;

	public int nofsensors = 7;

	public double[] gyro = new double[3];

	public double[][] accelerometer = new double[7][];

	public int fall;

	internal DateTime time;

	public double[] orientation = null;

	public double[] gravity = null;

	public SensorData(uint index, int nofsensors = 7)
	{
		this.index = index;
		this.nofsensors = nofsensors;
		for (int i = 0; i < 7; i++)
		{
			accelerometer[i] = new double[3];
		}
	}
}
