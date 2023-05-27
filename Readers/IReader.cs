using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Readers;
internal interface IReader
{
}

internal abstract class ReaderBase 
{
    public string FileName { get; protected set; } = string.Empty;
}

// Все эти классы призваны, чтобы получить из .xls, .xlsx, .csv файла массив объектов Chemical