using System.IO;
using System;
using chemicalParser;
using System.Drawing.Text;
using OfficeOpenXml;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Crypto.Generators;
using System.Data;


var filePath = "db.txt";
if (File.Exists(filePath) == false)
{
    Console.WriteLine("Файл не найден!");
    return;
}

Dictionary<string, string> NameAndInchiKeyDict = new Dictionary<string, string>();

//using(ExcelPackage package = new ExcelPackage(filePath))
//{
//    ExcelWorkbook wb = package.Workbook;
//    ExcelWorksheet sheet = wb.Worksheets[0];
//    var rows = sheet.Rows;
//    var cols = sheet.Columns;
//}

//return;

foreach (var line in File.ReadAllLines(filePath))
{
    var data = line.Split(':');
    if (NameAndInchiKeyDict.ContainsKey(data[0])) continue;
    NameAndInchiKeyDict.Add(data[0], data[1]);
}

var parser = new Parser(NameAndInchiKeyDict);
await parser.StartParsing();

Console.ReadKey();


void ReadExcelLowThan5()
{

}
void ReadExcelGreaterThan5()
{

}
void ReadCSV()
{

}