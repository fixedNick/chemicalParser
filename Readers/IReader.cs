using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using chemicalParser.Chemicals;


namespace chemicalParser.Readers;
internal interface IReader<T>
{
    public Task<T[]> Read(bool insertToDatabase);
    public void Save(T[] rows);
    void Append(T[] chemicals);
    void Create(T[] chemicals);
}

// Все эти классы призваны, чтобы получить из .xls, .xlsx, .csv файла массив объектов Chemical