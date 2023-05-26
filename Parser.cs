using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack.CssSelectors;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Threading;
using System.Threading.Tasks;

namespace chemicalParser;

internal class Parser
{
#pragma warning disable CS8618 
    private static Dictionary<string, string> NameKeyDict;
#pragma warning restore CS8618 

    private string baseWebsiteLink = @"https://webbook.nist.gov";
    private string baseElementAddress = @"https://webbook.nist.gov/cgi/cbook.cgi?InChI=";
    private string addressEnd = @"&Units=SI&cIR=on&cTZ=on";

    public Parser(Dictionary<string, string> dataDict)
    {
        NameKeyDict = dataDict ?? throw new Exception();
    }
    public async Task StartParsing()
    {
        foreach(var kvp in NameKeyDict)
        {
            // @"https://webbook.nist.gov/cgi/cbook.cgi?InChI= kvp.Value &Units=SI&cIR=on&cTZ=on"
            var linkBuilder = new StringBuilder();
            linkBuilder.Append(baseElementAddress);
            linkBuilder.Append(kvp.Value);
            linkBuilder.Append(addressEnd);

            // Создаем объект HtmlWeb
            HtmlWeb web = new HtmlWeb();
            // Загружаем HTML-страницу
            HtmlDocument document = web.Load(linkBuilder.ToString());

            var isIROnBasePage = await SearchForJCAMPFileLinkAndDownload(document, kvp.Key, linkBuilder.ToString());
            if (isIROnBasePage == true) continue;

            var linkTags = document.QuerySelectorAll("#IR-Spec ~ ul > li > a");
            foreach(var linkTag in linkTags)
            {
                var link = linkTag.GetAttributeValue("href", string.Empty);
                if (link.Contains("ID=") && link.Contains("#IR-SPEC"))
                {
                    var encodedLink = new StringBuilder().Append(baseWebsiteLink).Append(link).ToString();
                    var decodedLink = System.Net.WebUtility.HtmlDecode(encodedLink);
                    HtmlDocument IRDoc = web.Load(decodedLink);
                    await SearchForJCAMPFileLinkAndDownload(IRDoc, kvp.Key, decodedLink);
                }
            }

        }
    }

    private static string DownloadsDirectory = "downloads";
    private async Task DownloadFile(string link, string name)
    {
        if (Directory.Exists(DownloadsDirectory) == false) Directory.CreateDirectory(DownloadsDirectory);

        var renamedFile = GetFreeFileName(DownloadsDirectory, name);

        using(HttpClient httpClient = new HttpClient())
        {
            var decodedLink = System.Net.WebUtility.HtmlDecode(new StringBuilder().Append(baseWebsiteLink).Append(link).ToString());
            byte[] fileData = await httpClient.GetByteArrayAsync(decodedLink);
            await File.WriteAllBytesAsync($"{DownloadsDirectory}/{renamedFile}", fileData);
            await Console.Out.WriteLineAsync($"Скачан файл для {name}, файл: {renamedFile}");
        }
    }

    private string GetFreeFileName(string downloadsDirectory, string name)
    {
        var files = Directory.GetFiles(DownloadsDirectory);
        int counter = 0;
        while(true)
        {
            var fileName = $"{name}_{counter}.jdx";
            if (File.Exists($"{downloadsDirectory}/{fileName}") == true)
                counter++;
            else return $"{fileName}";
        }
    }

    private async Task<bool> SearchForJCAMPFileLinkAndDownload(HtmlDocument IRDoc, string name, string link)
    {
        bool isLinkFound = false;

        List<HtmlNode>? tableRows = IRDoc.QuerySelectorAll("table tr").ToList();
        if (tableRows == null || tableRows.Count <= 0)
            return await Task.FromResult(false);

        foreach(var row in tableRows)
        {
            var tableHeader = row.QuerySelector("th");
            if (tableHeader == null) continue;

            if (tableHeader.InnerText.Trim().ToLower().Contains("resolution"))
            {
                var tableData = row.QuerySelector("td");
                if (tableData == null) continue;

                if (double.TryParse(tableData.InnerText.Replace('.', ',').Trim(), out double val) == true)
                {
                    if (val < 2.0d)
                    {
                        await Console.Out.WriteLineAsync($"{name}. Найденный IR спектр имеет разрешение меньше 2.0. [{val}]");
                        return await Task.FromResult(false);
                    }
                }
                else
                {
                    var normalResults = new[]
                    {
                        "1 CM-1 AT 4000",
                        "2 CM-1",
                        "2.0 cm-1",
                        "2-3 CM-1",
                        "4 CM-1"
                    };

                    if (normalResults.Where(res => res.ToLower().Equals(tableData.InnerText.ToLower().Trim())).FirstOrDefault() == null)
                    {
                        await Console.Out.WriteLineAsync($"{name} [{link}]. Информационный элемент Разрешение найден, но не удалось получить его значение [{tableData.InnerText}]");
                        return await Task.FromResult(false);
                    }
                }
            }
        }
        
        var textToSearch = "Download";

        var pTags = IRDoc.QuerySelectorAll("p").ToList();
        foreach(var p in pTags)
        {
            if (p.InnerText.ToLower().Trim().Contains(textToSearch.ToLower()))
            {
                var downloadLink = p.QuerySelector("a").GetAttributeValue("href", string.Empty);
                DownloadFile(downloadLink, name).GetAwaiter().GetResult();
                isLinkFound = true;
            }
        }
       
        return await Task.FromResult(isLinkFound);
    }
}