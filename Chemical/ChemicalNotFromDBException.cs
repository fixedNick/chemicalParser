using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class ChemicalNotFromDBException : Exception
{
    public ChemicalNotFromDBException() : base("В базе данных не удалось найти такого химического соединения")
    {
        
    }
}
