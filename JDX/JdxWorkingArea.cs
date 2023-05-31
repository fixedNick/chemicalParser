using chemicalParser.Chemicals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.JDX;
internal class GraphWorkingArea
{
    public double MinX { get; private set; }
    public double MaxX { get; private set; }
    public double MinY { get; private set; }
    public double MaxY { get; private set; }
    public GraphWorkingArea(double minx, double maxx, double miny, double maxy)
    {
        MinX = minx;
        MaxX = maxx;
        MinY = miny;
        MaxY = maxy;
    }

    internal static GraphWorkingArea FromPoints(Point[] points, int maxWidth, int minWidth)
    {
        var minHeigt = points.Min(p => p.Y);
        var maxHeight = points.Max(p => p.Y);
        return new GraphWorkingArea(minWidth, maxWidth, minHeigt, maxHeight);
    }
}
