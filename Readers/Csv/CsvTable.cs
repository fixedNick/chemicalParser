using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Readers.Csv;

internal class CsvTable
{
    public int Rows { get; private set; }
    public int Columns { get; private set; }

    List<Cell> Data;

    public CsvTable(int rows, int columns, List<string[]>? data = null)
    {
        Rows = rows;
        Columns = columns;
        Data = new List<Cell>();

        for (int r = 0; r < data?.Count; r++)
            for (int c = 0; c < data[r].Length; c++)
                Data.Add(new Cell(r, c, data[r][c]));
    }

    public Cell this[int row, int col]
    {
        get
        {
            Cell? result = Data.Where(c => c.Row == row && c.Column == col).FirstOrDefault();
            if (result is null)
                throw new TableRangeException(row, col);
            return (Cell)result;
        }
        set 
        {
            if (row > Rows || col > Columns)
                throw new TableRangeException(row, col);

            GetCell(row,col).Value = value;
        }
    }

    private Cell GetCell(int row, int col)
    {
        foreach (var cell in Data)
        { 
            if (cell.Row == row && cell.Column == col)
                return cell;
        }
        throw new TableRangeException(row,col);
    }
}