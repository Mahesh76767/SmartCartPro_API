using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace SmartCartPro.DataAccess.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly string _connectionString;

        protected BaseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        protected MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

        protected async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object?> parameters)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteNonQueryAsync();
        }

        protected async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object?> parameters)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteScalarAsync();
        }

        private static void AddParameters(MySqlCommand cmd, Dictionary<string, object?> parameters)
        {
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        }
    }
}