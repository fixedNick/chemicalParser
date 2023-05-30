using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack.CssSelectors;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Threading;
using System.Threading.Tasks;

using chemicalParser.JDX;
using chemicalParser.SQL;
using chemicalParser.Chemicals;
using NPOI.OpenXmlFormats;

namespace chemicalParser.Parser;

internal class Parser
{
    public static readonly string DownloadsDirectory = "downloads";

    private readonly Chemical[] Chemicals;
    private bool InsertSpectresInDatabase = false;

    private string baseWebsiteLink = @"https://webbook.nist.gov";
    private string baseElementAddress = @"https://webbook.nist.gov/cgi/cbook.cgi?InChI=";
    private string addressEnd = @"&Units=SI&cIR=on&cTZ=on";

    public Parser(Chemical[] chemicals, bool insertSpectresInDatabase)
    {
        Chemicals = chemicals;
        InsertSpectresInDatabase = insertSpectresInDatabase;
    }
    public async Task StartParsing()
    {
        foreach (var chemical in Chemicals)
        {
            List<string> linksToCheck = new List<string>();

            await Console.Out.WriteLineAsync($"Начат парсинг элемента: {chemical.Info.EnName}");
            // @"https://webbook.nist.gov/cgi/cbook.cgi?InChI= kvp.Value &Units=SI&cIR=on&cTZ=on"
            var linkBuilder = new StringBuilder();
            linkBuilder.Append(baseElementAddress);
            linkBuilder.Append(chemical.Info.InChiKey.Trim());
            linkBuilder.Append(addressEnd);
            linksToCheck.Add(linkBuilder.ToString());
            // Создаем объект HtmlWeb
            HtmlWeb web = new HtmlWeb();
            // Загружаем HTML-страницу
            HtmlDocument document = web.Load(linkBuilder.ToString());

            //
            if (IsMultiValuesForInchiKey(document))
            {
                linksToCheck = GetLinksFromMultiPage(document);
                await Console.Out.WriteLineAsync($"Для элемента: {chemical.Info.EnName} найдено несколько ссылок [{linksToCheck.Count}] по InChiKey");

            }
            //
            int counter = 1;
            foreach (var mainDocLink in linksToCheck)
            {
                await Console.Out.WriteLineAsync($"Элемент: {chemical.Info.EnName}, проверка ссылки [{counter++}/{linksToCheck.Count}] {mainDocLink}");
                document = web.Load(mainDocLink);
                var isIROnBasePage = await SearchForJCAMPFileLinkAndDownload(document, chemical, mainDocLink);
                if (isIROnBasePage == true) continue;

                var linkTags = document.QuerySelectorAll("#IR-Spec ~ ul > li > a");
                foreach (var linkTag in linkTags)
                {
                    var link = linkTag.GetAttributeValue("href", string.Empty);
                    if (link.Contains("ID=") && link.Contains("#IR-SPEC"))
                    {
                        var encodedLink = new StringBuilder().Append(baseWebsiteLink).Append(link).ToString();
                        var decodedLink = System.Net.WebUtility.HtmlDecode(encodedLink);
                        HtmlDocument IRDoc = web.Load(decodedLink);
                        var res = await SearchForJCAMPFileLinkAndDownload(IRDoc, chemical, decodedLink);
                        if (res == false)
                            await Console.Out.WriteLineAsync($"Не удалось получить спектры для {chemical.Info.EnName}");
                    }
                }
            }

        }
    }

    private List<string> GetLinksFromMultiPage(HtmlDocument document)
    {
        var result = new List<string>();
        var aElementsList = document.QuerySelectorAll("ol > li > a");
        foreach (var a in aElementsList)
        {
            if (a.GetAttributeValue("href", string.Empty) != string.Empty)
                result.Add(baseWebsiteLink + a.GetAttributeValue("href", string.Empty));
        }
        return result;
    }

    private bool IsMultiValuesForInchiKey(HtmlDocument document)
    {
        HtmlNode h2Element = document.DocumentNode.SelectSingleNode("//h2[contains(text(), 'Matches found')]");
        return h2Element is not null;
    }

    private async Task DownloadFile(string link, Chemical chemical)
    {
        if (Directory.Exists(DownloadsDirectory) == false) Directory.CreateDirectory(DownloadsDirectory);

        var renamedFile = GetFreeFileName(DownloadsDirectory, chemical.Info.EnName);

        using (HttpClient httpClient = new HttpClient())
        {
            var decodedLink = System.Net.WebUtility.HtmlDecode(new StringBuilder().Append(baseWebsiteLink).Append(link).ToString());
            byte[] fileData = await httpClient.GetByteArrayAsync(decodedLink);
            await File.WriteAllBytesAsync($"{DownloadsDirectory}/{renamedFile}", fileData);
            await Console.Out.WriteLineAsync($"Скачан файл для {chemical.Info.EnName}, файл: {renamedFile}");
        }

        if (InsertSpectresInDatabase)
        {
            Jdx jdxInfo = new Jdx($"{DownloadsDirectory}/{renamedFile}");
            jdxInfo.ParseJdx();
            await Sql.InsertBaseSpectreFromJDX(jdxInfo, chemical.Info.Id);
        }
    }

    private string GetFreeFileName(string downloadsDirectory, string name)
    {
        var files = Directory.GetFiles(DownloadsDirectory);
        int counter = 0;
        while (true)
        {
            var fileName = $"{name}_{counter}.jdx";
            if (File.Exists($"{downloadsDirectory}/{fileName}") == true)
                counter++;
            else return $"{fileName}";
        }
    }

    private async Task<bool> SearchForJCAMPFileLinkAndDownload(HtmlDocument IRDoc, Chemical chemical, string link)
    {
        bool isLinkFound = false;

        List<HtmlNode>? tableRows = IRDoc.QuerySelectorAll("table tr").ToList();
        if (tableRows == null || tableRows.Count <= 0)
            return await Task.FromResult(false);

        foreach (var row in tableRows)
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
                        await Console.Out.WriteLineAsync($"{chemical.Info.EnName}. Найденный IR спектр имеет разрешение меньше 2.0. [{val}]");
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
                        await Console.Out.WriteLineAsync($"{chemical.Info.EnName} [{link}]. Информационный элемент Разрешение найден, но не удалось получить его значение [{tableData.InnerText}]");
                        return await Task.FromResult(false);
                    }
                }
            }
        }

        var textToSearch = "Download";

        var pTags = IRDoc.QuerySelectorAll("p").ToList();
        foreach (var p in pTags)
        {
            if (p.InnerText.ToLower().Trim().Contains(textToSearch.ToLower()))
            {
                var downloadLink = p.QuerySelector("a").GetAttributeValue("href", string.Empty);
                DownloadFile(downloadLink, chemical).GetAwaiter().GetResult();
                isLinkFound = true;
            }
        }

        return await Task.FromResult(isLinkFound);
    }
}