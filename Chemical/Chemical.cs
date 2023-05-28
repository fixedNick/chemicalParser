using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chemicalParser.Sql;

namespace chemicalParser.Chemicals;


// DI
internal class Chemical
{
    public ChemicalInfo Info { get; set; }


    public Chemical(ChemicalInfo info)
    {
        Info = info;
    }

    public async Task<Spectre[]> GetSpectres()
    {
        var result = await Sql.Sql.GetChemicalSpectres(Info.Id);
        return await Task.FromResult(result);
    }
}