using System.Collections;
using System.Collections.Generic;
using OxyPlot;

namespace PufferFish;

internal class GraphDataSource
{
	public ArrayList[] data = new ArrayList[24];

	public ArrayList[] main = new ArrayList[6];

	public ArrayList[] orientation = new ArrayList[3];

	public ArrayList[] gravity = new ArrayList[3];

	public ArrayList speedData = new ArrayList();

	internal IEnumerable<DataPoint> getSensorData(int sensor, int axis)
	{
		for (int i = 0; i < data[sensor * 3 + axis].Count; i++)
		{
			yield return (DataPoint)data[sensor * 3 + axis][i];
		}
	}

	internal IEnumerable<DataPoint> getMainData(int sensor, int axis)
	{
		for (int i = 0; i < main[sensor * 3 + axis].Count; i++)
		{
			yield return (DataPoint)main[sensor * 3 + axis][i];
		}
	}

	internal IEnumerable<DataPoint> getSpeedData()
	{
		for (int i = 0; i < speedData.Count; i++)
		{
			yield return (DataPoint)speedData[i];
		}
	}

	internal IEnumerable<DataPoint> getGyroData(int axis)
	{
		for (int i = 0; i < data[21 + axis].Count; i++)
		{
			yield return (DataPoint)data[21 + axis][i];
		}
	}

	internal IEnumerable<DataPoint> getOrientationData(int axis)
	{
		for (int i = 0; i < orientation[axis].Count; i++)
		{
			yield return (DataPoint)orientation[axis][i];
		}
	}

	internal IEnumerable<DataPoint> getGravityData(int axis)
	{
		for (int i = 0; i < gravity[axis].Count; i++)
		{
			yield return (DataPoint)gravity[axis][i];
		}
	}

	internal void reset()
	{
		for (int i = 0; i < 24; i++)
		{
			data[i] = new ArrayList();
		}
		for (int j = 0; j < 6; j++)
		{
			main[j] = new ArrayList();
		}
		gravity[0] = new ArrayList();
		gravity[1] = new ArrayList();
		gravity[2] = new ArrayList();
		orientation[0] = new ArrayList();
		orientation[1] = new ArrayList();
		orientation[2] = new ArrayList();
		speedData = new ArrayList();
	}

	internal int getDataCount()
	{
		return data[0].Count;
	}

	internal void add(SensorData sd)
	{
		if (sd.nofsensors == 2)
		{
			for (int axis = 0; axis < 3; axis++)
			{
				main[axis].Add(new DataPoint(sd.index, sd.accelerometer[0][axis]));
			}
			for (int i = 0; i < 3; i++)
			{
				main[3 + i].Add(new DataPoint(sd.index, sd.gyro[i]));
			}
			return;
		}
		for (int sensor = 0; sensor < sd.nofsensors; sensor++)
		{
			for (int j = 0; j < 3; j++)
			{
				data[sensor * 3 + j].Add(new DataPoint(sd.index, sd.accelerometer[sensor][j]));
			}
		}
		if (sd.nofsensors == 7)
		{
			for (int k = 0; k < 3; k++)
			{
				data[sd.nofsensors * 3 + k].Add(new DataPoint(sd.index, sd.gyro[k]));
			}
		}
		if (sd.orientation != null)
		{
			orientation[0].Add(new DataPoint(sd.index, sd.orientation[0]));
			orientation[1].Add(new DataPoint(sd.index, sd.orientation[1]));
			orientation[2].Add(new DataPoint(sd.index, sd.orientation[2]));
		}
		if (sd.gravity != null)
		{
			gravity[0].Add(new DataPoint(sd.index, sd.gravity[0]));
			gravity[1].Add(new DataPoint(sd.index, sd.gravity[1]));
			gravity[2].Add(new DataPoint(sd.index, sd.gravity[2]));
		}
	}

	internal void add(GPSData sd)
	{
		speedData.Add(new DataPoint(sd.index, sd.speed));
	}
}
