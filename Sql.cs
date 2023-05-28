using chemicalParser.Chemicals;
using chemicalParser.Readers;
using MySqlConnector;
using NPOI.DDF;
using NPOI.SS.Formula;
using OfficeOpenXml.Sorting;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// TODO
/// Продумать работу с Chemical.Info, чтобы при внесении нового объекта в бд обратно получался и устанавливался его ID

namespace chemicalParser.Sql;

/// <summary>
/// Данный класс сконструирован для работы только с ОДНОЙ БАЗОЙ ДАННЫХ
/// Таблиц может быть сколько угодно
/// </summary>
/// 

static class Fields
{
    // TABLE ChemicalsTable
    public static readonly string ChemicalID = "ChemicalId";
    public static readonly string InChiKey = "InChiKey";
    public static readonly string CAS = "CAS";
    public static readonly string RuName = "RuName";
    public static readonly string EnName = "EnName";
    public static readonly string Formula = "Formula";

    public static readonly string SpectreID = "SpectreId";
    public static readonly string X = "X";
    public static readonly string Y = "Y";
}

internal static class Sql
{
    // TABLES
    private static readonly string ChemicalsTable = "ChemicalsInfo";
    private static readonly string SpectresTable = "Spectres";

    private static bool IsInitialized = false;
    private static string Server;
    private static string UserID;
    private static string Password;
    private static string Database;

    static Sql() { Server = UserID = Password = Database = string.Empty; }

    // Initialize setting before using this class
    public static void Initialize(string server, string uid, string pass, string db)
    {
        Server = server;
        UserID = uid;
        Password = pass;
        Database = db;

        IsInitialized = true;
    }

    /// <summary>
    /// Открывает соединение с БД
    /// ВАЖНО. Не забудь закрыть соединеие после использования
    /// await connection.CloseAsync();
    /// await connection.DisposeAsync();
    /// </summary>
    /// <returns>Объект открытого соединения</returns>
    /// <exception cref="Exception">SQL Settings isnt initialized</exception>
    public static async Task<MySqlConnection> OpenConnection()
    {
        if (IsInitialized == false)
            throw new Exception("Initialize your SQL Database. User method Sql.Initialize(db settings..);");

        var builder = new MySqlConnectionStringBuilder()
        {
            Server = Sql.Server,
            UserID = Sql.UserID,
            Password = Sql.Password,
            Database = Sql.Database
        };

        var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return await Task.FromResult(connection);
    }

    private static async Task<int> Insert(string sqlQuery)
    {
        var connection = await OpenConnection();

        var command = connection.CreateCommand();
        command.CommandText = sqlQuery;
        var rowsAdded = await command.ExecuteNonQueryAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
        return await Task.FromResult<int>(rowsAdded);
    }

    // Операция по внесению информации, которая сейчас в Excel в бд
    public static async void InsertChemicals(Chemical[] chemicals)
    {
        foreach (var chemical in chemicals)
        {
            await Insert(string.Format("INSERT INTO `{0}` (`{1}`,`{2}`,`{3}`,`{4}`,`{5}`) VALUE ('{6}','{7}','{8}','{9}','{10}')",
                ChemicalsTable,
                Fields.RuName, Fields.EnName, Fields.Formula, Fields.InChiKey, Fields.CAS,
                chemical.Info.RuName, chemical.Info.EnName, chemical.Info.Formula, chemical.Info.InChiKey, chemical.Info.Cas));
        }
    }

    // Данные в этой таблице хранятся как каждая отдельная X y точка каждого отдельного спектра
    // Предполагается, что у одного элемента может быть несколько спектров
    // Различаться для каждого из элементов они будут по Spectreidx
    // Пример: У метана есть 3 спектра
    // 0,1,2 - это SpectreId каждого из трех спектров для ChemicalID метана
    // ChemicalId совпадает с ID из таблицы ChemicalsTable
    // Важно понимать, что в у нас может быть так же и информация о допустим Этане, у которого тоже 3 спектра
    // При этом у каждого спектра Этана так же будут SpectreId 0,1,2
    // Потому что SpectreId относится к конкретному ChemicalID
    public static async void InsertSpectre(Spectre spectre)
    {
        foreach (var p in spectre.Points)
        {
            await Insert(string.Format("INSET INTO `{0}` (`{1}`,`{2}`,`{3}`,`{4}`) VALUES ('{5}', '{6}', '{7}', '{8}')",
                SpectresTable,
                Fields.ChemicalID, Fields.X, Fields.Y, Fields.SpectreID,
                spectre.ChemicalID, p.X, p.Y, spectre.SpectreID
                ));
        }       
    }

    
    /// ???
    public static async void InsertSingleJDX(JDXInfo jdx)
    {

    }


    /// <summary>
    /// Получает массив спектров для конкретного ChemicalId
    /// </summary>
    /// <param name="id">Chemical Id</param>
    /// <returns>Массив спектров</returns>
    public static async Task<Spectre[]> GetChemicalSpectres(int cid)
    {
        var connection = await OpenConnection();

        var query = string.Format("SELECT * FROM `{0}` WHERE `{1}` = '{2}'",
            SpectresTable,
            Fields.ChemicalID,
            cid);

        var cmd = connection.CreateCommand();
        cmd.CommandText = query;


        Dictionary<int, List<Point>> spectresInfo = new Dictionary<int, List<Chemicals.Point>>();

        var reader = await cmd.ExecuteReaderAsync();
        while(reader.ReadAsync().GetAwaiter().GetResult())
        {
            var spectreId = reader.GetInt32(Fields.SpectreID);
            var x = reader.GetDouble(Fields.X);
            var y = reader.GetDouble(Fields.Y);

            // Если такого ключа нет, то занимаем ключ и инитим лист поинтов
            if (spectresInfo.ContainsKey(spectreId) == false)
                spectresInfo[spectreId] = new List<Point>();
            
            spectresInfo[spectreId].Add(new Point(x, y));
        }

        
        var result = new Spectre[spectresInfo.Count];
        int resultIdx = 0;
        foreach(var s in spectresInfo)
        {
            // sort by x from ..400 to 4000..
            s.Value.Sort(Point.Empty);

            var spectre = new Spectre(cid, s.Key, s.Value.ToArray());
            result[resultIdx++] = spectre;
        }

        await connection.CloseAsync();
        await connection.DisposeAsync();

        return await Task.FromResult(result);
    }
}
