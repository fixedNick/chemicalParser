using chemicalParser.Chemicals;
using chemicalParser.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.JDX;

internal class Jdx
{
    private string FilePath;
    private static readonly string xFactorKey = "X";
    private static readonly string yFactorKey = "Y";

    public double XFactor
    {
        get => Factors.Where(f => f.Key.Equals(xFactorKey)).First().Value;
    }

    public double YFactor
    {
        get => Factors.Where(f => f.Key.Equals(yFactorKey)).First().Value;
    }
    public double DeltaX { get; private set; }

    public JdxPair[] Info;
    public Point[] GraphPoints;
    public GraphWorkingArea WorkingArea;

    private KeyValuePair<string, double>[] Factors;

    public Jdx(string filePath)
    {
        FilePath=filePath;
        ParseJdx();
    }

    private void ParseJdx()
    {
        var fileLines = new List<string>();
        using (var streamReader = new StreamReader(new FileStream(FilePath, FileMode.Open)))
        {
            while (streamReader.EndOfStream == false)
            {
                var line = streamReader.ReadLine();
                if (line is not null)
                    fileLines.Add(line);
            }

        }

        var jdxData = new List<string>();
        for (var i = 0; i < fileLines.Count; i++)
        {
            if (fileLines[i].Contains("Collection") || fileLines[i].Contains("United States") || fileLines[i].Contains("$"))
                continue;
            jdxData.Add(fileLines[i]);
        }

        List<JdxPair> pairs = new List<JdxPair>();
        List<Point> points = new List<Point>();

        foreach (var line in jdxData)
        {
            if (line.StartsWith("##"))
            {
                var splitted = line.Split('=');
                if (splitted.Length >= 2)
                    pairs.Add(new JdxPair(splitted[0].Replace("#", ""), splitted[1]));
            }
        }

        var xDeltaString = pairs.Where(p => p.Key.Trim().ToLower() == "deltax").FirstOrDefault()?.Value?.Replace('.', ',').Trim() ?? "1";
        var xDelta = double.Parse(xDeltaString);

        double xFactor = 1;
        var xFactorPair = pairs.Where(p => p.Key.Trim().ToLower() == "xfactor").FirstOrDefault();
        if (xFactorPair is not null)
            xFactor = Convert.ToDouble(xFactorPair.Value.Trim().Replace('.', ','));

        double yFactor = 1;
        var yFactorPair = pairs.Where(p => p.Key.Trim().ToLower() == "yfactor").FirstOrDefault();
        if (yFactorPair is not null)
            yFactor = Convert.ToDouble(yFactorPair.Value.Trim().Replace('.', ','));

        Factors = new KeyValuePair<string, double>[] {
            new KeyValuePair<string,double>(xFactorKey, xFactor),
            new KeyValuePair<string, double>(yFactorKey, yFactor)
        };
        this.DeltaX = xDelta;

        var isDataSection = false;
        foreach (var line in jdxData)
        {
            if (line.StartsWith("##XYDATA=(X++(Y..Y))"))
                isDataSection = true;

            if (isDataSection == false) continue;

            if (line.StartsWith("#")) continue;

            var splitted = line.Split(' ');
            var x = double.Parse(splitted[0].Replace('.', ',').Trim()) / xFactor;
            for (int i = 1; i < splitted.Length; i++)
            {
                var y = double.Parse(splitted[i].Replace('.', ',').Trim()) * yFactor;
                points.Add(new Point(x, y));
                x += xDelta;
            }
        }

        Info = pairs.ToArray();
        GraphPoints = points.ToArray();

        var min_X = pairs.Where(p => p.Key.Trim().ToLower() == "minx").FirstOrDefault();
        var max_X = pairs.Where(p => p.Key.Trim().ToLower() == "maxx").FirstOrDefault();
        var min_Y = pairs.Where(p => p.Key.Trim().ToLower() == "miny").FirstOrDefault();
        var max_Y = pairs.Where(p => p.Key.Trim().ToLower() == "maxy").FirstOrDefault();
        if (min_X != null && max_X  != null && max_Y != null && min_Y != null)
        {
            WorkingArea = new GraphWorkingArea(
                Convert.ToDouble(min_X.Value.Replace('.', ',').Trim()),
                Convert.ToDouble(max_X.Value.Replace('.', ',').Trim()),
                Convert.ToDouble(min_Y.Value.Replace('.', ',').Trim()),
                Convert.ToDouble(max_Y.Value.Replace('.', ',').Trim()));
        }
        else throw new NotFoundException("Min/Max values not found");
    }
}