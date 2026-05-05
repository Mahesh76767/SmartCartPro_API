using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Entities;
using SmartCartPro.Models.Enums;
using System.Reflection.PortableExecutable;
namespace SmartCartPro.DataAccess.Repositories
{
    // TODO: Implement AuthRepository
    public class AuthRepository : BaseRepository , IAuthRepository
    {
        public AuthRepository(IConfiguration config) : base(config) { }


        // ── GET USER BY EMAIL ──────────────────────────────────
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT 
                      UserId, Email, PasswordHash, FirstName,
                      LastName, Role, IsActive, CreatedAt
                    FROM Users
                    WHERE Email = @email AND IsActive = 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

                        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync()) return null;

                            return new User
                            {
                                UserId = reader.GetInt32("UserId"),
                                Email = reader.GetString("Email"),
                                PasswordHash = reader.GetString("PasswordHash"),
                                FirstName = reader.GetString("FirstName"),
                                LastName = reader.GetString("LastName"),
                                Role = Enum.Parse<UserRole>(reader.GetString("Role")),
                                IsActive = reader.GetBoolean("IsActive"),
                                CreatedAt = reader.GetDateTime("CreatedAt")
                            };
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }

        // ── GET USER BY ID ─────────────────────────────────────
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var sql = @"SELECT UserId, Email, PasswordHash, FirstName,
                               LastName, Role, IsActive, CreatedAt
                        FROM Users
                        WHERE UserId = @id AND IsActive = 1";

            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", userId);
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? MapUser(r) : null;
        }

        // ── CREATE USER ────────────────────────────────────────
        public async Task<int> CreateUserAsync(User user)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"INSERT INTO USERS (Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedAt, UpdatedAt)
                    VALUES
                        (@email, @hash, @first, @last, @role, 1, NOW(), NOW());
                    SELECT LAST_INSERT_ID();";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("email", user.Email);
                        cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
                        cmd.Parameters.AddWithValue("@first", user.FirstName);
                        cmd.Parameters.AddWithValue("@last", user.LastName);
                        cmd.Parameters.AddWithValue("@role", user.Role.ToString());

                        return Convert.ToInt32( await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error occured while insert: {ex.Message}");
                throw;
            }
        }

        // ── CHECK EMAIL EXISTS ─────────────────────────────────
        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT COUNT(*) FROM Users WHERE Email = @email";

                    using(MySqlCommand cmd = new MySqlCommand( query, con))
                    {
                        cmd.Parameters.AddWithValue("@email", email);

                        return Convert.ToInt32( await cmd.ExecuteScalarAsync()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured: {ex.Message}");
                throw;
            }
        }

        // ── SAVE REFRESH TOKEN ─────────────────────────────────
        public async Task SaveRefreshTokenAsync(int userId, string token, DateTime expiry)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"INSERT INTO RefreshTokens (UserId, Token, Expiry, IsRevoked, CreatedAt)
                    VALUES (@userId, @token, @expiry, 0, NOW())";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.Add("@token", MySqlDbType.Text).Value = token;
                        cmd.Parameters.AddWithValue("@Expiry", expiry);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured: {ex.Message}");
                throw;
            }
        }

        // ── GET REFRESH TOKEN ──────────────────────────────────
        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT TokenId, UserId, Token, Expiry, IsRevoked, CreatedAt
                        FROM RefreshTokens
                        WHERE Token = @token AND IsRevoked = 0";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using(MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        cmd.Parameters.Add("@token", MySqlDbType.Text).Value = token;

                        if (!await reader.ReadAsync()) return null;

                        return new RefreshToken
                        {
                            TokenId = reader.GetInt32("TokenId"),
                            UserId = reader.GetInt32("UserId"),
                            Token = reader.GetString("Token"),
                            Expiry = reader.GetDateTime("Expiry"),
                            IsRevoked = reader.GetBoolean("IsRevoked"),
                            CreatedAt = reader.GetDateTime("CreatedAt")
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured: {ex.Message}");
                throw;
            }
        }


        // ── REVOKE REFRESH TOKEN ───────────────────────────────
        public async Task RevokeRefreshTokenAsync(string token)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @token";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@token", MySqlDbType.Text).Value = token;

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured: {ex.Message}");
                throw;
            }
        }

        // ── UPDATE LAST LOGIN ──────────────────────────────────
        public async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE Users SET LastLoginAt = NOW() WHERE UserId = @id";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured: {ex.Message}");
                throw;
            }
        }

        // ── PRIVATE MAPPER ─────────────────────────────────────
        private static User MapUser(MySqlDataReader r) => new()
        {
            UserId = r.GetInt32("UserId"),
            Email = r.GetString("Email"),
            PasswordHash = r.GetString("PasswordHash"),
            FirstName = r.GetString("FirstName"),
            LastName = r.GetString("LastName"),
            Role = Enum.Parse<UserRole>(r.GetString("Role")),
            IsActive = r.GetBoolean("IsActive"),
            CreatedAt = r.GetDateTime("CreatedAt")
        };
    }
}