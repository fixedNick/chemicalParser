using chemicalParser.Chemicals;
using chemicalParser.Readers;
using MySqlConnector;
using OfficeOpenXml.Sorting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser;

/// <summary>
/// Данный класс сконструирован для работы только с ОДНОЙ БАЗОЙ ДАННЫХ
/// Таблиц может быть сколько угодно
/// </summary>
/// 

internal static class Sql
{
    private static string ChemicalsTable = "ChemicalsInfo";
    private static string SpectresTable = "Spectes";

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
                "RunName", "EnName", "Formula", "InChiKey", "CAS",
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
            await Insert(string.Format("INSET INTO `{0}` (`{1}`,`{2}`,`{3}`, `{4}`) VALUES ('{5}', '{6}', '{7}', '{8}')",
                SpectresTable,
                "ChemicalId", "X", "Y", "SpectreId",
                spectre.ChemicalID, p.X, p.Y, spectre.SpectreID
                ));
        }
    }

    public static async void InsertSingleJDX(JDXInfo jdx)
    {

    }
}
