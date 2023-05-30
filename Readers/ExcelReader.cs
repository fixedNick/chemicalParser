using chemicalParser.Chemicals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Readers;

internal class ExcelReader<T> : ReaderBase/*, chemicalParser.Readers.IReader<T>*/
{
    public ExcelReader(string fileName)
    {
        FilePath = fileName;
    }
}
