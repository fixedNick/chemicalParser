using System.IO;
using System;
using chemicalParser;
using System.Drawing.Text;

var filePath = "db.txt";
if (File.Exists(filePath) == false)
{
    Console.WriteLine("Файл не найден!");
    return;
}

Dictionary<string, string> NameAndInchiKeyDict = new Dictionary<string, string>();

foreach (var line in File.ReadAllLines(filePath))
{
    var data = line.Split(':');
    if (NameAndInchiKeyDict.ContainsKey(data[0])) continue;
    NameAndInchiKeyDict.Add(data[0], data[1]);
}

var parser = new Parser(NameAndInchiKeyDict);
await parser.StartParsing();

Console.ReadKey();