using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;


// DI
internal class Chemical
{
    public ChemicalInfo Info { get; set; }

    public Chemical(ChemicalInfo info)
    {
        Info = info;
    }
}