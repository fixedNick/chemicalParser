using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Readers;

internal abstract class ReaderBase
{
    public string FilePath { get; protected set; } = string.Empty;
}