using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class JDXInfo
{
    public GraphWorkingArea WorkingArea;
    public Point[] GraphPoints;
    public JDXInfo(Point[] points, GraphWorkingArea workingArea)
    {
        GraphPoints = points;
        WorkingArea = workingArea;
    }
}

class GraphWorkingArea
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
}