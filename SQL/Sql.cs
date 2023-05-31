using MySqlConnector;

using chemicalParser.JDX;
using chemicalParser.Readers;
using chemicalParser.Chemicals;
using NPOI.POIFS.EventFileSystem;
using chemicalParser.Exceptions;

namespace chemicalParser.SQL;

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
    public static readonly string ChemicalType = "ChemicalType";

    public static readonly string SpectreID = "SID";
    public static readonly string X = "X";
    public static readonly string Y = "Y";
    public static readonly string XFactor = "XFactor";
    public static readonly string YFactor = "YFactor";
    public static readonly string DeltaX = "YFactor";
}

static class Tables
{
    public static readonly string Chemicals = "ChemicalsInfo";
    public static readonly string CIDtoSID = "CIDtoSID";
    public static readonly string BaseSpectres = "Spectres";
    public static readonly string ZippedSpectres = "SpectresZipped";
    public static readonly string SpectresInfo = "SpectresInfo";
}
internal static class Sql
{
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
    public static async Task<int> InsertChemicals(Chemical[] chemicals)
    {
        var rowsAdded = 0;
        foreach (var chemical in chemicals)
        {
            rowsAdded = await Insert(string.Format("INSERT INTO `{0}` (`{1}`,`{2}`,`{3}`,`{4}`,`{5}`, `{6}`) VALUE ('{7}','{8}','{9}','{10}','{11}', '{12}')",
                Tables.Chemicals,
                Fields.RuName, Fields.EnName, Fields.Formula, Fields.InChiKey, Fields.CAS, Fields.ChemicalType,
                chemical.Info.RuName, chemical.Info.EnName, chemical.Info.Formula, chemical.Info.InChiKey, chemical.Info.Cas, (int)chemical.Info.ChemicalType));
        }
        return await Task.FromResult(rowsAdded);
    }

    /// <summary>
    /// boxedSpectreInfo - это объект типа InsertSpectreInfo, запакованный в object
    /// с его помощью можно добавить конкретный спектр либо в zip, либо в базовую таблицу
    /// </summary>
    /// <param name="boxedSpectreInfo">объект типа InsertSpectreInfo</param>
    public static async void InsertSpectre(object? boxedSpectreInfo)
    {
        if (boxedSpectreInfo is null) return;

        using var connection = await OpenConnection();

        var spectreInfo = (InsertSpectreInfo)boxedSpectreInfo;
        var spectre = spectreInfo.Spectre;
        var isZipped = spectreInfo.IsZip;

        foreach (var p in spectre.Points)
        {
            var query = string.Format("INSERT INTO `{0}` (`{1}`,`{2}`,`{3}`,`{4}`) VALUES ('{5}', {6}, {7}, '{8}')",
                isZipped ? Tables.ZippedSpectres : Tables.BaseSpectres,
                Fields.ChemicalID, Fields.X, Fields.Y, Fields.SpectreID,
                spectre.ChemicalID, p.X.ToString().Replace(',', '.'), p.Y.ToString().Replace(',', '.'), spectre.SpectreID
                );
            using var command = connection.CreateCommand();
            command.CommandText = query;
            await command.ExecuteNonQueryAsync();
        }

        await connection.CloseAsync();
    }

    private static object insertJdxLockObject = new object();
    public static async Task InsertBaseSpectreFromJDX(Jdx jdx, int chemicalId)
    {
        // Добавить новый Spectre
        lock (insertJdxLockObject)
        {
            using var connection = OpenConnection().GetAwaiter().GetResult();
            int maxID = -1;

            // Проверить какой максимальный ID у спектров конкретного ChemicalID
            string queryGetMaxSID = @$"SELECT MAX({Fields.SpectreID}) 
                   FROM {Tables.BaseSpectres}";

            using var command = connection.CreateCommand();
            command.CommandText = queryGetMaxSID;
            var result = command.ExecuteScalar();
            try
            {
                maxID = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                maxID++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                connection.Close();
                connection.Dispose();
                return;
            }

            connection.Close();
            connection.Dispose();

            var spectre = new Spectre(chemicalId, maxID, jdx.GraphPoints, jdx.YFactor, jdx.XFactor, jdx.DeltaX);
            var insertSpectreInfo = new InsertSpectreInfo(spectre, false);

            Thread insertThread = new Thread(new ParameterizedThreadStart(InsertSpectre));
            insertThread.Start(insertSpectreInfo);
        }
        await Task.FromResult(1);
    }

    /// <summary>
    /// Получает массив спектров для конкретного ChemicalId
    /// </summary>
    /// <param name="id">Chemical Id</param>
    /// <returns>Массив спектров</returns>
    public static async Task<Spectre[]> GetChemicalSpectres(int cid, bool zipped = false)
    {
        using var connection = await OpenConnection();

        var queryGetSpectreIds = $"SELECT `{Fields.SpectreID}` FROM `{Tables.CIDtoSID}` WHERE `{Fields.ChemicalID}` = '{cid}'";
        List<int> sids = new List<int>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = queryGetSpectreIds;
            var sidsReader = await cmd.ExecuteReaderAsync();
            while (await sidsReader.ReadAsync())
                sids.Add(sidsReader.GetInt32(Fields.SpectreID));
        }

        Dictionary<int, List<Point>> spectresInfo = new Dictionary<int, List<Chemicals.Point>>();

        foreach (var sid in sids)
        {
            var queryGetSpectres = string.Format("SELECT * FROM `{0}` WHERE `{1}` = '{2}'",
                zipped ? Tables.ZippedSpectres : Tables.BaseSpectres,
                Fields.SpectreID,
                sid);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = queryGetSpectres;
            using var spectreReader = await cmd.ExecuteReaderAsync();
            var spectreId = spectreReader.GetInt32(Fields.SpectreID);
            var x = spectreReader.GetDouble(Fields.X);
            var y = spectreReader.GetDouble(Fields.Y);

            if (spectresInfo.ContainsKey(spectreId) == false)
                spectresInfo.Add(spectreId, new List<Point>());

            spectresInfo[spectreId].Add(new Point(x, y));
        }

        var result = new Spectre[spectresInfo.Count];
        int resultIdx = 0;
        foreach (var spectreKeyValue in spectresInfo)
        {
            spectreKeyValue.Value.Sort(Point.Empty);
            var spectreInfo = await GetSpectreInfo(spectreKeyValue.Key);
            result[resultIdx++] = new Spectre(cid, spectreKeyValue.Key, spectreKeyValue.Value.ToArray(), spectreInfo);
        }

        await connection.CloseAsync();
        return await Task.FromResult(result);
    }
    public static async Task<Spectre> GetSpectre(int sid, bool isZipped = false)
    {
        using var connection = await OpenConnection();

        var getSpectreQuery = string.Format("SELECT * FROM `{0}` WHERE `{1}` = '{2}'",
                                            isZipped ? Tables.ZippedSpectres : Tables.BaseSpectres,
                                            Fields.SpectreID,
                                            sid);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = getSpectreQuery;
        using var pointsReader = await cmd.ExecuteReaderAsync();

        var points = new List<Point>();

        while (await pointsReader.ReadAsync())
        {
            var x = pointsReader.GetDouble(Fields.X);
            var y = pointsReader.GetDouble(Fields.Y);

            points.Add(new Point(x, y));
        }
        await connection.CloseAsync();
        var cid = await GetCIDFromSID(sid);
        var spectreInfo = await GetSpectreInfo(sid);

        var resultSpectre = new Spectre(cid, sid, points.ToArray(), spectreInfo);
        return await Task.FromResult(resultSpectre);
    }

    public static async Task<SpectreInfo> GetSpectreInfo(int sid)
    {
        using var connection = await OpenConnection();
        SpectreInfo spectreInfo;

        using var cmd = connection.CreateCommand();
        var getSIQuery = string.Format("SELECT * FROM `{0}` WHERE `{1}` = '{2}'",
                            Tables.SpectresInfo,
                            Fields.SpectreID, sid);
        cmd.CommandText = getSIQuery;

        using var siReader = await cmd.ExecuteReaderAsync();
        while (await siReader.ReadAsync())
        {
            var yFactor = siReader.GetDouble(Fields.YFactor);
            var xFactor = siReader.GetDouble(Fields.XFactor);
            var xDelta = siReader.GetDouble(Fields.DeltaX);

            await connection.CloseAsync();
            spectreInfo = new SpectreInfo(xFactor, yFactor, xDelta);
            return await Task.FromResult(spectreInfo);
        }
        await connection.CloseAsync();
        throw new NotFoundException($"Не удалось найти spectreInfo в бд для SID {sid}");
    }

    public static async Task<int> GetCIDFromSID(int sid)
    {
        using var connection = await OpenConnection();

        var getCIDfromSIDQuery = string.Format("SELECT `{0}` FROM `{1}` WHERE `{2}` = '{3}'",
                                                Fields.ChemicalID,
                                                Tables.CIDtoSID,
                                                Fields.SpectreID,
                                                sid);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = getCIDfromSIDQuery;
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var cid = reader.GetInt32(Fields.ChemicalID);
            await connection.CloseAsync();
            return await Task.FromResult(cid);
        }
        await connection.CloseAsync();
        throw new NotFoundException("Min/Max values not found");
    }


    /// <summary>
    /// Метод для получения всех химических элементов из базы данных
    /// </summary>
    /// <returns></returns>
    public static async Task<Chemical[]> GetChemicals()
    {
        var result = new List<Chemical>();
        using var connection = await OpenConnection();
        var query = string.Format("SELECT * FROM `{0}`", Tables.Chemicals);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        using var reader = await cmd.ExecuteReaderAsync();
        while (reader.ReadAsync().GetAwaiter().GetResult())
        {
            var cid = reader.GetInt32(Fields.ChemicalID);
            var ruName = reader.GetString(Fields.RuName);
            var enName = reader.GetString(Fields.EnName);
            var formula = reader.GetString(Fields.Formula);
            var inchikey = reader.GetString(Fields.InChiKey);
            var cas = reader.GetString(Fields.CAS);
            var type = reader.GetInt32(Fields.ChemicalType);
            ChemicalInfo cInfo = new ChemicalInfo(cid, ruName, enName, formula, inchikey, cas, (ChemicalInfo.ChemicalsType)type);
            Chemical chemical = new Chemical(cInfo);
            result.Add(chemical);
        }
        await connection.CloseAsync();
        return await Task.FromResult(result.ToArray());
    }

    public static async Task<int> GetNextChemicalID()
    {
        var connection = await OpenConnection();
        var cmd = connection.CreateCommand();

        var query = string.Format("SELECT MAX({0}) FROM {1}", Fields.ChemicalID, Tables.Chemicals);
        cmd.CommandText = query;

        var result = await cmd.ExecuteScalarAsync();
        int nextID = result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;


        await connection.CloseAsync();
        await connection.DisposeAsync();

        return await Task.FromResult(nextID);
    }

    public static async Task<bool> IsChemicalInDatabase(string formula, string inchikey, string cas, int ctype)
    {
        var connection = await OpenConnection();
        var cmd = connection.CreateCommand();

        var query = string.Format("SELECT * FROM `{0}` WHERE `{1}` = '{2}' AND `{3}` = '{4}' AND `{5}` = '{6}' AND `{7}` = '{8}'",
            Tables.Chemicals,
            Fields.Formula, formula,
            Fields.InChiKey, inchikey,
            Fields.CAS, cas,
            Fields.ChemicalType, ctype);
        cmd.CommandText = query;
        var reader = await cmd.ExecuteReaderAsync();
        var result = reader.HasRows;

        await connection.CloseAsync();
        await connection.DisposeAsync();

        return await Task.FromResult(result);
    }
}