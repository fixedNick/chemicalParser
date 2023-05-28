using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class Point : IComparer<Point>
{
    public static readonly Point Empty = new Point(0, 0);

    public double X;
    public double Y;

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public int Compare(Point? x, Point? y)
    {
        if (x is null || y is null)
            throw new ArgumentException("Cannot compare with null object");
        return x.X.CompareTo(y.X);
    }
}
