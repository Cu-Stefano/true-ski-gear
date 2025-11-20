using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using OxyPlot;

namespace PufferFish;

public abstract class BaseSession
{
	public const int SESSION_V1 = 1;

	public const int SESSION_V2 = 2;

	public const int maxNumberOfGraphs = 9;

	public const int maxNumberOfSensors = 7;

	public int sessionVersion = 1;

	public int nofgraphs;

	public int nofsensors;

	public ArrayList falls = new ArrayList();

	internal ArrayList gps_data = new ArrayList(5000);

	internal DateTime maxTime;

	internal DateTime minTime;

	public static int gyro_index = 5;

	public static int speed_index = 6;

	internal SQLiteConnection sessionDBConn;

	internal GraphDataSource graphData = new GraphDataSource();

	public uint MinIndex { get; internal set; }

	public uint MaxIndex { get; internal set; }

	public DateTime MaxTime => minTime.AddMilliseconds(MaxIndex - MinIndex);

	public DateTime MinTime => minTime;

	public int getGPSDataCount => gps_data.Count;

	public int getGyroIndex()
	{
		return gyro_index;
	}

	public int getSpeedIndex()
	{
		return speed_index;
	}

	public BaseSession(int nofgraphs, int nofsensors)
	{
		this.nofgraphs = Math.Min(9, nofgraphs);
		this.nofsensors = Math.Min(7, nofsensors);
		MinIndex = 0u;
		MaxIndex = 0u;
	}

	internal Range<uint> GetSessionRange()
	{
		return new Range<uint>
		{
			start = MinIndex,
			end = MaxIndex + 50
		};
	}

	internal abstract void loadData(int minTime, int maxTime);

	public virtual bool isNewCriticalFallFormat()
	{
		return true;
	}

	internal DateTime getTimeForDataIndex(uint selecteDataIndex)
	{
		return minTime.AddMilliseconds(selecteDataIndex - MinIndex);
	}

	internal int getGPSIndex(uint selecteDataIndex)
	{
		for (int i = 0; i < gps_data.Count - 1; i++)
		{
			if (((GPSData)gps_data[i + 1]).index >= selecteDataIndex)
			{
				return i;
			}
		}
		return Math.Max(0, gps_data.Count - 1);
	}

	internal string filename()
	{
		return sessionDBConn.FileName;
	}

	public abstract void CloseDB();

	public abstract void commit();

	public abstract void exportFromTo(string FileName, int minIndex, int maxIndex);

	public IEnumerable<DataPoint> getSensorData(int sensor, int axis)
	{
		return graphData.getSensorData(sensor, axis);
	}

	public IEnumerable<DataPoint> getMainData(int sensor, int axis)
	{
		return graphData.getMainData(sensor, axis);
	}

	public IEnumerable<DataPoint> getSpeedData()
	{
		return graphData.getSpeedData();
	}

	public IEnumerable<DataPoint> getGyroData(int axis)
	{
		return graphData.getGyroData(axis);
	}

	internal GPSData getLastGPSdata()
	{
		try
		{
			return (GPSData)gps_data[gps_data.Count - 1];
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal GPSData getGPSdata(int index)
	{
		return (GPSData)gps_data[index];
	}

	public static string getDBFolder()
	{
		return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Pufferfish\\db\\";
	}

	public double getMaxLat()
	{
		double max = 0.0;
		foreach (GPSData gpd in gps_data)
		{
			if (gpd.coords.Lat > max)
			{
				max = gpd.coords.Lat;
			}
		}
		return max;
	}

	public double getMaxLng()
	{
		double max = 0.0;
		foreach (GPSData gpd in gps_data)
		{
			if (gpd.coords.Lng > max)
			{
				max = gpd.coords.Lng;
			}
		}
		return max;
	}

	public double getMinLat()
	{
		double min = 0.0;
		foreach (GPSData gpd in gps_data)
		{
			if (gpd.coords.Lat < min)
			{
				min = gpd.coords.Lat;
			}
		}
		return min;
	}

	public double getMinLng()
	{
		double min = 0.0;
		foreach (GPSData gpd in gps_data)
		{
			if (gpd.coords.Lng < min)
			{
				min = gpd.coords.Lng;
			}
		}
		return min;
	}

	public LinkedList<Tag> getTags()
	{
		LinkedList<Tag> ret = new LinkedList<Tag>();
		SQLiteCommand command = new SQLiteCommand("SELECT id, type, description, Timestamp from tags", sessionDBConn);
		SQLiteDataReader reader = command.ExecuteReader();
		try
		{
			while (reader.Read())
			{
				ret.AddLast(new Tag(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)));
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			reader.Close();
		}
		return ret;
	}
}
