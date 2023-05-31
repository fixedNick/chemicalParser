using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class SpectreInfo
{
    public double XFactor { get; private set; }
    public double YFactor { get; private set; }
    public double DeltaX { get; private set; }

    public SpectreInfo(double xfactor, double yfactor, double deltax)
    {
        XFactor = xfactor;
        YFactor = yfactor;
        DeltaX = deltax;
    }
}
