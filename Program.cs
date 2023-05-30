using chemicalParser.SQL;
using chemicalParser.Parser;
using chemicalParser.Readers;
using chemicalParser.Chemicals;

Sql.Initialize("db4free.net", "wqgwqg", "fgewgwfgewgw", "fgewgw");

IReader<Chemical> reader = new CsvReader<Chemical>(filePath: "test.csv", startRowIndex: 1);
var chemicals = await reader.Read(insertToDatabase: true);

var chemicalsFromDatabase = await Sql.GetChemicals();

var parser = new Parser(chemicals, insertSpectresInDatabase: true);
await parser.StartParsing();

List<Spectre[]> spectres = new List<Spectre[]>();
foreach(var c in chemicals)
    spectres.Add(await c.GetSpectres());

while (true)
    Thread.Sleep(5000);
