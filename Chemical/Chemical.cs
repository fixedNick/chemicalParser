using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chemicalParser.SQL;
using NPOI.OpenXmlFormats.Wordprocessing;

namespace chemicalParser.Chemicals;

// DI
internal class Chemical
{
    public ChemicalInfo Info { get; set; }
    private Spectre[]? Spectres { get; set; }

    public Chemical(ChemicalInfo info)
    {
        Info = info;
    }

    public static async Task<Chemical[]> GetChemicalsFromDatabase()
    {
        var result = await Sql.GetChemicals();
        return await Task.FromResult(result);
    }

    /// <summary>
    /// Метод получает массив спектров, которые относятся к данному химическому соединению.
    /// При длине поля Spectres[] = 0 - Выполнит вопрос к БД и заполнить данный массив
    /// Если в массиве Spectres[] есть какое-то спектр - вернет ссылку на массив
    /// </summary>
    /// <param name="forceSelectFromSql">При true в любому случае выполнить запрос к БД и перезапишет массив Spectres</param>
    /// <returns>Ссылка на массив Spectres</returns>
    public async Task<Spectre[]> GetSpectres(bool forceSelectFromSql = false)
    {
        if (forceSelectFromSql || Spectres is null || Spectres.Length == 0)
            Spectres = await Sql.GetChemicalSpectres(Info.Id);
        
        return await Task.FromResult(Spectres);
    }

    /// <summary>
    /// Пытается сопоставить chemicalID и химический элемент из базы данных
    /// </summary>
    /// <param name="chemicalID">ID Соединения</param>
    /// <returns>Тип химического соединения</returns>
    /// <exception cref="ChemicalNotFromDBException">В базе данных не удалось найти такого химического соединения</exception>
    public static ChemicalInfo.ChemicalsType GetChemicalType(int chemicalID)
    {
        foreach(var chemical in Sql.GetChemicals().GetAwaiter().GetResult())
        {
            if (chemical.Info.Id == chemicalID)
                return chemical.Info.ChemicalType;
        }
        throw new ChemicalNotFromDBException();
    }

    /// <summary>
    /// Пытается сопоставить имя и химический элемент из базы данных
    /// </summary>
    /// <param name="inchikey">Хешированный ключ химического соединения</param>
    /// <returns>Тип химического соединения</returns>
    /// <exception cref="ChemicalNotFromDBException">В базе данных не удалось найти такого химического соединения</exception>
    public static ChemicalInfo.ChemicalsType GetChemicalType(string inchikey)
    {
        foreach (var chemical in Sql.GetChemicals().GetAwaiter().GetResult())
        {
            if (inchikey == chemical.Info.InChiKey)
                return chemical.Info.ChemicalType;
        }
        throw new ChemicalNotFromDBException();
    }

    /// <summary>
    /// Пытается сопоставить имя и химический элемент из базы данных
    /// </summary>
    /// <param name="name">Название химического соединения</param>
    /// <param name="ruLang">TRUE - Русское название, FALSE - Английское название</param>
    /// <returns>Тип химического соединения</returns>
    /// <exception cref="ChemicalNotFromDBException">В базе данных не удалось найти такого химического соединения</exception>
    public static ChemicalInfo.ChemicalsType GetChemicalType(string name, bool ruLang = true)
    {
        foreach (var chemical in Sql.GetChemicals().GetAwaiter().GetResult())
        {
            if ((ruLang && name == chemical.Info.RuName) || (ruLang == false && name == chemical.Info.EnName))
                return chemical.Info.ChemicalType;
        }
        throw new ChemicalNotFromDBException();
    }
}