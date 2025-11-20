using System.Collections;

namespace PufferFish;

public class GPSDataComparer : IComparer
{
	int IComparer.Compare(object a, object b)
	{
		GPSData aa = (GPSData)a;
		GPSData bb = (GPSData)b;
		return aa.index.CompareTo(bb.index);
	}
}
