using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace PufferFish;

internal class MyGMapRoute : GMapRoute
{
	public int middlepoint = -1;

	public Pen Stroke2 = new Pen(Color.Red);

	public MyGMapRoute(string name)
		: base(name)
	{
		Stroke2.Width = 2f;
	}

	public MyGMapRoute(IEnumerable<PointLatLng> points, string name)
		: base(points, name)
	{
	}

	protected MyGMapRoute(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override void OnRender(Graphics g)
	{
		if (base.IsVisible && LocalPoints.Count > 1)
		{
			int m = ((middlepoint < 0 || middlepoint >= LocalPoints.Count) ? LocalPoints.Count : middlepoint);
			int A = m - 10;
			int B = m + 10;
			renderGradientUpFrom(g, A, m);
			renderGradientDownFrom(g, m, B);
		}
	}

	private void renderFrom(Graphics g, int start, int end)
	{
		if (end < 0 || start == end || start >= LocalPoints.Count)
		{
			return;
		}
		if (start < 0)
		{
			start = 0;
		}
		if (end >= LocalPoints.Count)
		{
			end = LocalPoints.Count - 1;
		}
		int len = end - start;
		if (len >= 1)
		{
			Stroke.Color = getColor(0);
			Point[] pnts = new Point[len];
			for (int i = 0; i < len; i++)
			{
				Point p2 = new Point((int)LocalPoints[start + i].X, (int)LocalPoints[start + i].Y);
				pnts[len - 1 - i] = p2;
			}
			if (pnts.Length > 1)
			{
				g.DrawLines(Stroke, pnts);
			}
		}
	}

	private void renderGradientUpFrom(Graphics g, int start, int end)
	{
		if (end < 0 || start == end || start >= LocalPoints.Count)
		{
			return;
		}
		if (start < 0)
		{
			start = 0;
		}
		if (end >= LocalPoints.Count)
		{
			end = LocalPoints.Count - 1;
		}
		int len = end - start;
		if (len >= 1)
		{
			Point p1 = new Point((int)LocalPoints[start].X, (int)LocalPoints[start].Y);
			for (int i = 1; i <= len; i++)
			{
				Point p2 = new Point((int)LocalPoints[start + i].X, (int)LocalPoints[start + i].Y);
				Stroke.Color = getColor(i);
				g.DrawLine(Stroke, p1, p2);
				p1 = p2;
			}
		}
	}

	private void renderGradientDownFrom(Graphics g, int start, int end)
	{
		if (end < 0 || start == end || start >= LocalPoints.Count)
		{
			return;
		}
		if (start < 0)
		{
			start = 0;
		}
		if (end >= LocalPoints.Count)
		{
			end = LocalPoints.Count - 1;
		}
		int len = end - start;
		if (len >= 1)
		{
			Point p1 = new Point((int)LocalPoints[start].X, (int)LocalPoints[start].Y);
			for (int i = 1; i <= len; i++)
			{
				Point p2 = new Point((int)LocalPoints[start + i].X, (int)LocalPoints[start + i].Y);
				Stroke.Color = getColor(len - i);
				g.DrawLine(Stroke, p1, p2);
				p1 = p2;
			}
		}
	}

	private Color getColor(int step)
	{
		return Color.FromArgb(125 + 13 * step, Color.MidnightBlue.R + (Color.IndianRed.R - Color.MidnightBlue.R) / 9 * step, Color.MidnightBlue.G + (Color.IndianRed.G - Color.MidnightBlue.G) / 9 * step, Color.MidnightBlue.B + (Color.IndianRed.B - Color.MidnightBlue.B) / 9 * step);
	}
}
