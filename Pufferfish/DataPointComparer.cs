using System.Collections;
using OxyPlot;

namespace PufferFish;

public class DataPointComparer : IComparer
{
	int IComparer.Compare(object a, object b)
	{
		DataPoint aa = (DataPoint)a;
		DataPoint bb = (DataPoint)b;
		return aa.X.CompareTo(bb.X);
	}
}
