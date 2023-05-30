using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Readers.Csv;

internal class TableRangeException : Exception
{
    public TableRangeException(int row, int col) : base($"Table has no cell at row[{row}] and col[{col}]") { }
}
