using chemicalParser.Chemicals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.SQL;

internal class InsertSpectreInfo
{
    public readonly Spectre Spectre;
    public readonly bool IsZip;

    public InsertSpectreInfo(Spectre spectre, bool isZip)
    {
        Spectre=spectre;
        IsZip=isZip;
    }
}
