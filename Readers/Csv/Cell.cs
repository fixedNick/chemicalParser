using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Readers.Csv;

class Cell
{
    public object Value;
    public int Row { get; private set; }
    public int Column { get; private set; }

    public Cell(int r, int c, object v)
    {
        Row = r; Column = c; Value = v;
    }

    public T As<T>()
    {
        if (typeof(T) == typeof(Int16))
            return (T)(object)Convert.ToInt16(Value);
        if (typeof(T) == typeof(Int32))
            return (T)(object)Convert.ToInt32(Value);
        if (typeof(T) == typeof(Int64))
            return (T)(object)Convert.ToInt64(Value);
        if (typeof(T) == typeof(Double))
            return (T)(object)Convert.ToDouble(Value);
        if (typeof(T) == typeof(Boolean))
            return (T)(object)Convert.ToBoolean(Value);

        return (T)Value;
    }

    public int AsInt()
    {
        return (int) Value;
    }
}