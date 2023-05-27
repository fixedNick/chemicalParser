using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class Spectre
{
    public int ChemicalID { get; private set; }
    public int SpectreID { get; private set; }
    Point[] Points;
    public Spectre(int cid, int sid, Point[] points)
    {
        Points = points; 
        ChemicalID = cid;
        SpectreID = sid;
    }
}
