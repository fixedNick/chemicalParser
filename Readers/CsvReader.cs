using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using chemicalParser.SQL;
using chemicalParser.Chemicals;
using chemicalParser.Readers.Csv;
using static chemicalParser.Chemicals.ChemicalInfo;

namespace chemicalParser.Readers;
class CsvReader<T> : ReaderBase, IReader<T>
{
    private int StartRowIndex;
    private int StartColumnIndex;
    private string Delimiter;
    public CsvReader(string filePath, int startRowIndex = 0, int startColumnIndex = 0, string delimiter = ",")
    {
        FilePath = filePath;
        Delimiter = delimiter;
        StartRowIndex = startRowIndex;
        StartColumnIndex = startColumnIndex;
    }

    public void Append(T[] chemicals)
    {
        throw new NotImplementedException();
    }

    public void Create(T[] chemicals)
    {
        throw new NotImplementedException();
    }

    public void Save(T[] rows)
    {
    }

    async Task<T[]> IReader<T>.Read(bool insertToDatabase)
    {
        T[]? result = null;

        var table = GetTable();

        if (typeof(T) == typeof(Chemical))
            result = await GetChemicals(table, insertToDatabase) as T[];

        result = result ?? Enumerable.Empty<T>().ToArray();
        return await Task.FromResult(result);
    }

    private async Task<Chemical[]> GetChemicals(CsvTable table, bool insertToDatabase)
    {
        List<Chemical> chemicals = new List<Chemical>();
        for (int i = StartRowIndex; i < table.Rows; i++)
        {
            var ruName = table[i, 0].As<string>();
            var enName = table[i, 1].As<string>();
            var formula = table[i, 2].As<string>();
            var inchikey = table[i, 3].As<string>();
            var cas = table[i, 4].As<string>();
            var ctype = (ChemicalsType)table[i, 5].As<int>();
           
            var id = -1;
            Chemical chemical;
            if (insertToDatabase)
            {
                var isChemicalInDatabase = await Sql.IsChemicalInDatabase(formula, inchikey, cas, (int)ctype);
                if (isChemicalInDatabase == false)
                {
                    id = await Sql.GetNextChemicalID();
                    Console.WriteLine($"Элемента {enName} не найдено в БД. Назначен ID: {id}");
                }
                else Console.WriteLine($"Элемент {enName} уже есть в бд");

                chemical = new Chemical(
                    new ChemicalInfo(id, enName, enName, formula, inchikey, cas, (ChemicalInfo.ChemicalsType)ctype));

                if (isChemicalInDatabase == false)
                    await Sql.InsertChemicals(new Chemical[] { chemical });
            }
            else chemical = new Chemical(
                    new ChemicalInfo(id, enName, enName, formula, inchikey, cas, (ChemicalInfo.ChemicalsType)ctype));

            chemicals.Add(chemical);
        }
        return chemicals.ToArray();
    }

    /// <summary>
    /// Конвертирует CSV файл в объект CsvTable, после чего к его элементам можно обращаться как к ячейкам
    /// </summary>
    /// <returns>Таблица CSV</returns>
    private CsvTable GetTable()
    {
        var data = new List<string[]>();
        try
        {
            using (StreamReader reader = new StreamReader(FilePath))
            {
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    string[] row = line.Split(Delimiter);

                    var formattedRow = line.Split(Delimiter).ToList().GetRange(StartColumnIndex, row.Length - StartColumnIndex).ToArray();
                    data.Add(formattedRow);
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine("Error reading the file: " + e.Message);
        }

        return new CsvTable(data.Count, data.Max(r => r.Length), data);
    }
}