using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class ChemicalInfo
{
    public enum ChemicalsType : int
    {
        Alkanes = 0,
        Alkenes = 1,
        Carbonides = 2,
        some = 3,
        oth = 4, 
        cyclo = 5
    }

    public int Id { get; private set; }
    public string RuName { get; private set; }
    public string EnName { get; private set; }
    public string Formula { get; private set; }
    public string InChiKey { get; private set; }
    public string Cas { get; private set; }
    public ChemicalsType ChemicalType { get; private set; } 

    public ChemicalInfo(int id, string ruName, string enName, string formula, string inchikey, string cas, ChemicalsType type)
    {
        Id = id;
        RuName = ruName;
        EnName = enName;
        Formula = formula;
        InChiKey = inchikey;
        Cas = cas;
        ChemicalType = type;
    }
}