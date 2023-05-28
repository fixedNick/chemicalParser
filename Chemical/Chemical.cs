using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chemicalParser.SQL;

namespace chemicalParser.Chemicals;


// DI
internal class Chemical
{
    public ChemicalInfo Info { get; set; }


    public Chemical(ChemicalInfo info)
    {
        Info = info;
    }

    public static async Task<Chemical[]> GetChemicalsFromDatabase()
    {
        var result = await Sql.GetChemicalsInfo();
        return await Task.FromResult(result);
    }

    public async Task<Spectre[]> GetSpectres()
    {
        var result = await Sql.GetChemicalSpectres(Info.Id);
        return await Task.FromResult(result);
    }
}