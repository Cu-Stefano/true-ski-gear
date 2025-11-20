using System;
using GMap.NET;

namespace PufferFish;

public class GPSData
{
	public uint index;

	public PointLatLng coords;

	public float speed;

	public float angle;

	public DateTime time;

	public GPSData(uint index, DateTime time, float speed)
	{
		this.index = index;
		this.speed = speed;
		this.time = time;
	}
}
