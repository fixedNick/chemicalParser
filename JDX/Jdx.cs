using chemicalParser.Chemicals;
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

    public JdxPair[] Info;
    public Point[] GraphPoints;
    public GraphWorkingArea WorkingArea;

    public Jdx(string filePath)
    {
        FilePath=filePath;
    }

    public void ParseJdx()
    {
        var fileLines = new List<string>();
        using (var streamReader = new StreamReader(new FileStream(FilePath, FileMode.Open)))
        {
            while(streamReader.EndOfStream == false)
            {
                var line = streamReader.ReadLine();
                if(line is not null)
                    fileLines.Add(line);
            }
           
        }

        var jdxData = new List<string>();
        for (var i = 0; i < fileLines.Count; i++)
        {
            if (fileLines[i].Contains("Collection") || fileLines[i].Contains("United States"))
                continue;
            jdxData.Add(fileLines[i]);
        }

        List<JdxPair> pairs = new List<JdxPair>();
        List<Point> points = new List<Point>();

        foreach(var line in jdxData)
        {
            if (line.StartsWith("##"))
            {
                var splitted = line.Split('=');
                if(splitted.Length >= 2)
                    pairs.Add(new JdxPair(splitted[0].Replace("#", ""), splitted[1]));
            }
            else if(line is not null)
            {
                var splitted = line.Split(' ');
                if (splitted.Length >= 2)
                {
                    if(double.TryParse(splitted[0].Replace('.', ','), out double x) && double.TryParse(splitted[1].Replace('.', ','), out double y)) points.Add(new Point(x,y));  
                }
            }
        }

        double xFactor = 1;
        var xFactorPair = pairs.Where(p => p.Key.Trim().ToLower() == "xfactor").FirstOrDefault();
        if(xFactorPair is not null)
            xFactor = Convert.ToDouble(xFactorPair.Value.Trim().Replace('.',','));

        double yFactor = 1;
        var yFactorPair = pairs.Where(p => p.Key.Trim().ToLower() == "yfactor").FirstOrDefault();
        if (yFactorPair is not null)
            yFactor = Convert.ToDouble(yFactorPair.Value.Trim().Replace('.', ','));

        for(var i = 0; i < points.Count; i++)
        {
            points[i].X *= xFactor;
            points[i].Y *= yFactor;
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
        else throw new ValuesNotFoundException();
    }
}