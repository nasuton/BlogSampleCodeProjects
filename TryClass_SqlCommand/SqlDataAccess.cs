using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;

public class SqlDataAccess : IDisposable
{
    /// <summary>
    /// 使用するコネクションオブジェクトを格納する
    /// </summary>
    private SqlConnection _connection;
    /// <summary>
    /// 使用するトランザクションオブジェクトを格納する
    /// </summary>
    private SqlTransaction _transaction;
    /// <summary>
    /// トランザクションを使用するかどうか示す値を格納する
    /// </summary>
    private bool _useTransaction;
    /// <summary>
    /// ロールバックを実施したかどうか示す値を格納する
    /// </summary>
    private bool _doneRollback;
    /// <summary>
    /// コネクションやコマンドタイムアウト時間
    /// </summary>
    private int _timeOut;
    
    /// <summary>
    /// アンマネージリソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行する
    /// </summary>
    /// <remarks>
    /// トランザクションを使用する設定になっている場合はトランザクションを破棄する
    /// </remarks>
    public virtual void Dispose()
    {
        if (_transaction != null && _useTransaction)
        {
            if (!_doneRollback)
            {
                try
                {
                    _transaction.Commit();
                }
                catch
                {
                    _transaction.Rollback();
                    throw;
                }
            }
            _transaction.Dispose();
        }
        _connection?.Close();
    }
    /// <summary>
    /// コンストラクタ。引数の値を使用してコネクションを開く
    /// </summary>
    public SqlDataAccess(string sqlServerName, string dbName, bool persistSecurityInfo, string connectUserID, string password, int timeOut = 30, bool useTransaction = false)
    {
        _timeOut = timeOut;
        string connectionString = BuildConnectionString(sqlServerName, dbName, persistSecurityInfo, connectUserID, password);
        // コネクションを開く
        OpenConnection(connectionString, useTransaction);
    }
    /// <summary>
    /// 接続文字列を構築する
    /// </summary>
    /// <param name="sqlServerName">対象SQLサーバー名</param>
    /// <param name="dbName">対象DB名</param>
    /// <param name="persistSecurityInfo">パスワードやアクセス トークンなどのセキュリティに依存する情報の保持方法</param>
    /// <param name="connectUserID">SQLへ接続時に使用するユーザー名</param>
    /// <param name="password">SQLへ接続時に使用するユーザーのパスワード</param>
    /// <returns>接続情報文字列</returns>
    private string BuildConnectionString(string sqlServerName, string dbName, bool persistSecurityInfo, string connectUserID, string password)
    {
        var conn = new SqlConnectionStringBuilder
        {
            DataSource = sqlServerName,
            InitialCatalog = dbName,
            PersistSecurityInfo = persistSecurityInfo,
            TrustServerCertificate = true, // 証明書の検証を行わない
            Encrypt = true, // 暗号化を有効にする
            UserID = connectUserID,
            Password = password,
            ConnectTimeout = _timeOut
        };
        return conn.ConnectionString;
    }
    /// <summary>
    /// 接続文字列とトランザクションを使用するかどうか示す値を使用してコネクションを開く
    /// </summary>
    /// <remarks>
    /// トランザクションを使用する場合は既定の分離レベルが使用されます。
    /// </remarks>
    /// <param name="connectionString">接続文字列</param>
    /// <param name="useTransaction">トランザクションを使用するかどうか示す値</param>
    public void OpenConnection(string connectionString, bool useTransaction)
    {
        _connection = new SqlConnection(connectionString);
        _connection.Open();
        _useTransaction = useTransaction;
        if (useTransaction)
        {
            _transaction = _connection.BeginTransaction();
        }
    }
    /// <summary>
    /// SQLクエリを実行し、結果を取得する
    /// </summary>
    /// <param name="sqlText">実行するSQL文</param>
    /// <param name="parameters">クエリに渡すパラメータ</param>
    /// <param name="readAction"></param>
    public void ExecuteReader(string sqlText, List<SqlParameter> parameters = null, Action<SqlDataReader> readAction = null)
    {
        try
        {
            using (var command = _connection.CreateCommand())
            {
                if (_useTransaction)
                {
                    command.Transaction = _transaction;
                }
                command.CommandText = sqlText;
                command.CommandTimeout = _timeOut;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                using (var reader = command.ExecuteReader())
                {
                    readAction?.Invoke(reader);
                }
            }
        }
        catch
        {
            if (_transaction != null)
            {
                _doneRollback = true;
            }
            throw;
        }
    }
    /// <summary>
    /// クエリを実行し、影響を受けた行数を返す
    /// </summary>
    /// <param name="sqlText">SQLテキスト</param>
    /// <param name="parameters">パラメータリスト</param>
    /// <returns>影響を受けた行数</returns>
    public int ExecuteNonQuery(string sqlText, List<SqlParameter> parameters = null)
    {
        try
        {
            using (var command = _connection.CreateCommand())
            {
                if (_useTransaction)
                {
                    command.Transaction = _transaction;
                }
                // SQLコマンドの設定
                command.CommandText = sqlText;
                command.CommandTimeout = _timeOut;
                // パラメータの設定
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                var result = command.ExecuteNonQuery();
                command.Parameters.Clear();
                return result;
            }
        }
        catch
        {
            if (_transaction != null)
            {
                _doneRollback = true;
            }
            throw;
        }
    }
    /// <summary>
    /// テーブルの存在チェック
    /// </summary>
    /// <param name="tableName">テーブル名(論理名)</param>
    /// <param name="table_schema">対象テーブルのスキーマ名</param>
    /// <returns>対象テーブルの存在有無</returns>
    public bool ExistTableInDB(string tableName, string table_schema = "dbo")
    {
        string sql = $"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{table_schema}' AND TABLE_NAME = '{tableName}'";
        bool exists = false;
        ExecuteReader(sql, null, reader =>
        {
            exists = reader.HasRows;
        });
        return exists;
    }
}
