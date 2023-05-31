using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using chemicalParser.JDX;
using chemicalParser.Exceptions;
using chemicalParser.Utils;

namespace chemicalParser.Chemicals;

internal class Spectre
{
    private static int MaxWidth = 4000;
    private static int MinWidth = 400;

    public SpectreInfo SpectreInfo { get; private set; }

    public int ChemicalID { get; private set; }
    public int SpectreID { get; private set; }
    public Point[] Points;
    public Spectre(int cid, int sid, Point[] points, SpectreInfo spectreInfo)
    {
        Points = points;
        ChemicalID = cid;
        SpectreID = sid;
        this.SpectreInfo = spectreInfo;
    }
    public Spectre(int cid, int sid, Point[] points, double yFactor, double xFactor, double deltax) 
        : this(cid,sid,points, new SpectreInfo(xFactor, yFactor, deltax)) { }
    public int[] GetZipHeights()
    {
        var bitmap = GetSpectreAsImages();
        var heights = GetCoordinatesWithStep(bitmap);
        return heights.ToArray();
    }

    private Bitmap GetSpectreAsImages()
    {
        string imagesDirectory = "images";
        if (!Directory.Exists(imagesDirectory))
            Directory.CreateDirectory(imagesDirectory);

        var workingArea = GraphWorkingArea.FromPoints(Points, MaxWidth, MinWidth);

        double width = workingArea.MaxX - workingArea.MinX;
        double height = workingArea.MaxY - workingArea.MinY;
        height *= 1000;

        using Bitmap bitmap = new Bitmap((int)width, (int)height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        // Очистка фона
        graphics.Clear(System.Drawing.Color.White);

        using Pen pointPen = new(Color.Black, 2);
        for (int i = 0; i < Points.Length - 1; i++)
        {
            var firstPoint =
            new System.Drawing.Point((int)(Points[i].X), ((int)(Points[i].Y * 1000)));
            var secondPoint =
                new System.Drawing.Point((int)(Points[i + 1].X), ((int)(Points[i + 1].Y * 1000)));

            if (SpectreInfo.YFactor == 1)
            {
                firstPoint = new System.Drawing.Point(firstPoint.X, 1000 - firstPoint.Y);
                secondPoint = new System.Drawing.Point(secondPoint.X, 1000 - secondPoint.Y);
            }

            graphics.DrawLine(pointPen, firstPoint, secondPoint);
        }

        var freeFileName = Util.GetFreeFileName(imagesDirectory, SpectreID.ToString(), extension: ".png", returnPath: true);
        bitmap.Save(freeFileName, ImageFormat.Png);
        return bitmap;
    }

    private List<int> GetCoordinatesWithStep(Bitmap graphImage)
    {
        if (graphImage is null) throw new NullReferenceException("graphImage is null");

        var coordinates = new List<System.Drawing.Point>();
        var transformedCoordinates = new List<System.Drawing.Point>();
        var width = graphImage.Width;
        var height = graphImage.Height;

        int convStep = 10 * width / (MaxWidth - MinWidth);
        for (var x = 0; x < width; x += convStep)
        {
            for (var y = 0; y < height; y++)
            {
                var pixel = graphImage.GetPixel(x, y);
                if (pixel.R <= 120 && pixel.G <= 120 && pixel.B <= 120) { coordinates.Add(new System.Drawing.Point(x, y)); break; }
            }
        }

        try
        {
            transformedCoordinates = TransformCoordinates(coordinates, height, width, MaxWidth, MinWidth);
        }
        catch (BadInputException exc)
        {
            Console.WriteLine(exc.Message);
        }

        var exampleForClassification = GetClassificationCoordinates(transformedCoordinates);
        var item = ConvertDataToArray(exampleForClassification);
        return item;
    }

    private List<System.Drawing.Point> TransformCoordinates(List<System.Drawing.Point> coordenates, int height, int width, int userWidthMax, int userWidthMin)
    {
        List<System.Drawing.Point> TransformedCoordinates = new List<System.Drawing.Point>();

        foreach (System.Drawing.Point currentCoordinate in coordenates)
        {
            var transfCoordinateX = (-1) * ((currentCoordinate.X * (userWidthMax - userWidthMin) / width) - (userWidthMax - userWidthMin));
            var transfCoordinateY = (height - currentCoordinate.Y) * 100 / height;
            if ((transfCoordinateX <= 4000) && (transfCoordinateX >= 400))
                TransformedCoordinates.Add(new System.Drawing.Point(transfCoordinateX, transfCoordinateY));
        }

        return TransformedCoordinates;
    }

    private List<System.Drawing.Point> GetClassificationCoordinates(List<System.Drawing.Point> coordinates)
    {
        List<System.Drawing.Point> transformedDataForClassification = new List<System.Drawing.Point>();
        for (int i = 4000; i >= 400; i -= 10)
        {
            int iter = 0;
            int difference = i;
            int iterMinDifference = 0;

            foreach (var data in coordinates)
            {
                int a = Math.Abs(i - Math.Abs(coordinates[iter].X));

                if (a == 0)
                {
                    transformedDataForClassification.Add(coordinates[iter]);
                    iter++;
                    difference = 0; break;


                }
                else if (a < difference)
                {
                    difference = a;
                    iterMinDifference = iter;
                    iter++;
                }
                else { iter++; }

            }
            if (difference != 0)
                transformedDataForClassification.Add(new System.Drawing.Point(i, coordinates[iterMinDifference].Y));
        }
        return transformedDataForClassification;
    }
    private List<int> ConvertDataToArray(List<System.Drawing.Point> NewData)
    {
        int cols = NewData.Count;
        List<int> newItem = new List<int>();
        for (int i = 0; i < cols; i++)
            newItem.Add(NewData[i].Y);
        return newItem;
    }
}
