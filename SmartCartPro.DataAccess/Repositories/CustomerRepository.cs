using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Customer;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.DataAccess.Repositories
{
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        public CustomerRepository(IConfiguration config) : base(config) { }

        // Get list of customers with paging, search, sort
        public async Task<PagedResult<CustomerResponseDto>> GetAllAsync(CustomerFilterDto filter)
        {
            var where = new List<string> { "c.IsActive = 1" };
            var @params = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                where.Add("(c.FirstName LIKE @s OR c.LastName LIKE @s OR c.Email LIKE @s)");
                @params["@s"] = $"%{filter.SearchTerm.Trim()}%";
            }

            var whereClause = $"WHERE {string.Join(" AND ", where)}";
            var orderBy = filter.SortBy switch
            {
                "loyaltyPoints" => "c.LoyaltyPoints DESC",
                "totalSpent" => "TotalSpent DESC",
                "totalOrders" => "TotalOrders DESC",
                _ => "c.CreatedAt DESC"
            };

            var countSql = $"SELECT COUNT(*) FROM Customers c {whereClause}";
            var dataSql = $@"
                SELECT c.*,
                    COUNT(o.OrderId) AS TotalOrders,
                    COALESCE(SUM(CASE WHEN o.Status != 'Cancelled' THEN o.TotalAmount ELSE 0 END), 0) AS TotalSpent
                FROM Customers c
                LEFT JOIN Orders o ON c.CustomerId = o.CustomerId
                {whereClause}
                GROUP BY c.CustomerId
                ORDER BY {orderBy}
                LIMIT @pageSize OFFSET @offset";

            var countParams = new Dictionary<string, object?>(@params);
            @params["@pageSize"] = filter.PageSize;
            @params["@offset"] = filter.Offset;

            using var conn = GetConnection();
            await conn.OpenAsync();

            using var countCmd = new MySqlCommand(countSql, conn);
            foreach (var p in countParams) countCmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            using var cmd = new MySqlCommand(dataSql, conn);
            foreach (var p in @params) cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

            var list = new List<CustomerResponseDto>();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(MapCustomer(r));

            return new PagedResult<CustomerResponseDto>
            { Data = list, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
        }

        public async Task<CustomerResponseDto?> GetByIdAsync(int id)
        {
            var sql = @"SELECT c.*,
                COUNT(o.OrderId) AS TotalOrders,
                COALESCE(SUM(CASE WHEN o.Status != 'Cancelled' THEN o.TotalAmount ELSE 0 END), 0) AS TotalSpent
                FROM Customers c LEFT JOIN Orders o ON c.CustomerId = o.CustomerId
                WHERE c.CustomerId = @id GROUP BY c.CustomerId";

            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            var customer = MapCustomer(r);
            await r.CloseAsync();

            var addrSql = "SELECT * FROM CustomerAddresses WHERE CustomerId = @id ORDER BY IsDefault DESC";
            using var addrCmd = new MySqlCommand(addrSql, conn);
            addrCmd.Parameters.AddWithValue("@id", id);
            using var ar = await addrCmd.ExecuteReaderAsync();
            while (await ar.ReadAsync())
                customer.Addresses.Add(new CustomerAddressDto
                {
                    AddressId = ar.GetInt32("AddressId"),
                    Street = ar.GetString("Street"),
                    City = ar.GetString("City"),
                    State = ar.GetString("State"),
                    ZipCode = ar.GetString("ZipCode"),
                    Country = ar.GetString("Country"),
                    IsDefault = ar.GetBoolean("IsDefault")
                });
            return customer;

        }

        public async Task<int> CreateAsync(CreateCustomerDto dto)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                    INSERT INTO Customers 
                    (FirstName, LastName, Email, Phone, LoyaltyPoints, IsActive, CreatedAt)
                    VALUES (@f, @l, @e, @p, 0, 1, NOW());
                    SELECT LAST_INSERT_ID();";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@f", MySqlDbType.VarChar).Value = dto.FirstName?.Trim() ?? "";
                        cmd.Parameters.Add("@l", MySqlDbType.VarChar).Value = dto.LastName?.Trim() ?? "";
                        cmd.Parameters.Add("@e", MySqlDbType.VarChar).Value = dto.Email?.ToLower().Trim() ?? "";
                        cmd.Parameters.Add("@p", MySqlDbType.VarChar).Value = (object?)dto.Phone ?? DBNull.Value;

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }

        public async Task<bool>UpdateAsync(int id, UpdateCustomerDto dto)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    var sets = new List<string>();

                    using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = con;

                        if(dto.FirstName != null)
                        {
                            sets.Add("FirstName = @f");
                            cmd.Parameters.Add("@f", MySqlDbType.VarChar).Value = dto.FirstName.Trim();
                        }

                        if (dto.LastName != null)
                        {
                            sets.Add("LastName = @l");
                            cmd.Parameters.Add("@l", MySqlDbType.VarChar).Value = dto.LastName.Trim();
                        }

                        if (dto.Email != null)
                        {
                            sets.Add("Email = @e");
                            cmd.Parameters.Add("@e", MySqlDbType.VarChar).Value = dto.Email.ToLower().Trim();
                        }

                        if (dto.Phone != null)
                        {
                            sets.Add("Phone = @ph");
                            cmd.Parameters.Add("@ph", MySqlDbType.VarChar).Value = dto.Phone;
                        }

                        if(sets.Count == 0)
                        {
                            return true;
                        }

                        string query = $@"
                        UPDATE Customers 
                        SET {string.Join(", ", sets)} 
                        WHERE CustomerId = @id";

                        cmd.CommandText = query;

                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                        int rows = Convert.ToInt32(await cmd.ExecuteNonQueryAsync());
                        return rows > 0;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE Customers SET IsActive = 0 WHERE CustomerId = @id";

                    using(MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        return rowsAffected > 0;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }




        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    var sql = excludeId.HasValue
                    ? "SELECT COUNT(*) FROM Customers WHERE Email=@e AND CustomerId!=@id"
                    : "SELECT COUNT(*) FROM Customers WHERE Email=@e";

                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@e", email.ToLower().Trim());

                        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@id", excludeId.Value);

                        var result = await cmd.ExecuteScalarAsync();

                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HasActiveOrdersAsync(int id)
        {
            try
            {
                using(var con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT COUNT(*) FROM Orders WHERE CustomerId = @id AND Status NOT IN ('Delivered', 'Cancelled')";

                    using(var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                        var result = await cmd.ExecuteScalarAsync();

                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }

        private static CustomerResponseDto MapCustomer(MySqlDataReader r) => new()
        {
            CustomerId = r.GetInt32("CustomerId"),
            FirstName = r.GetString("FirstName"),
            LastName = r.IsDBNull(r.GetOrdinal("LastName")) ? "" : r.GetString("LastName"),
            Email = r.GetString("Email"),
            Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString("Phone"),
            LoyaltyPoints = r.GetInt32("LoyaltyPoints"),
            TotalOrders = Convert.ToInt32(r["TotalOrders"]),
            TotalSpent = Convert.ToDecimal(r["TotalSpent"]),
            IsActive = r.GetBoolean("IsActive"),
            CreatedAt = r.GetDateTime("CreatedAt")
        };

        public async Task<List<CustomerOrderDto>> GetOrdersAsync(int id)
        {
            try
            {
                var resultList = new List<CustomerOrderDto>();
                using(var con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT o.OrderId, o.OrderDate, o.Status, o.TotalAmount,
                    COUNT(oi.OrderItemId) AS ItemCount
                    FROM Orders o LEFT JOIN OrderItems oi ON o.OrderId=oi.OrderId
                    WHERE o.CustomerId=@id GROUP BY o.OrderId ORDER BY o.OrderDate DESC ";

                    using(var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                        using(MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while(await reader.ReadAsync())
                            {
                                resultList.Add(new CustomerOrderDto
                                {
                                    OrderId = reader.GetInt32("OrderId"),
                                    OrderDate = reader.GetDateTime("OrderDate"),
                                    Status = reader.GetString("Status"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    ItemCount = reader.GetInt32("ItemCount")

                                });
                            }
                        }
                    }
                }
                return resultList;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ReviewResponseDto>> GetReviewsAsync(int customerId)
        {
            try
            {
                var resultList = new List<ReviewResponseDto>();
                using(var con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT pr.*, p.Name AS ProductName
                    FROM ProductReviews pr JOIN Products p ON pr.ProductId=p.ProductId
                    WHERE pr.CustomerId=@id ORDER BY pr.CreatedAt DESC";

                    using(var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = customerId;

                        using(MySqlDataReader r = await cmd.ExecuteReaderAsync())
                        {
                            while(await r.ReadAsync())
                            {
                                resultList.Add(new ReviewResponseDto
                                {
                                    ReviewId = r.GetInt32("ReviewId"),
                                    ProductId = r.GetInt32("ProductId"),
                                    ProductName = r.GetString("ProductName"),
                                    Rating = r.GetInt32("Rating"),
                                    Comment = r.IsDBNull(r.GetOrdinal("Comment")) ? null : r.GetString("Comment"),
                                    SentimentLabel = r.IsDBNull(r.GetOrdinal("SentimentLabel")) ? null : r.GetString("SentimentLabel"),
                                    SentimentScore = r.IsDBNull(r.GetOrdinal("SentimentScore")) ? null : r.GetDecimal("SentimentScore"),
                                    CreatedAt = r.GetDateTime("CreatedAt")

                                });
                            }
                        }
                    }
                }
                return resultList;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                throw;
            }
        }


        public async Task<int> AddReviewAsync(int customerId, CreateReviewDto dto, string? sentimentLabel, decimal? sentimentScore)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                INSERT INTO ProductReviews
                (ProductId, CustomerId, Rating, Comment, SentimentScore, SentimentLabel, CreatedAt)
                VALUES (@pid, @cid, @rating, @comment, @score, @label, NOW());
                SELECT LAST_INSERT_ID();";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@pid", MySqlDbType.Int32).Value = dto.ProductId;
                        cmd.Parameters.Add("@cid", MySqlDbType.Int32).Value = customerId;
                        cmd.Parameters.Add("@rating", MySqlDbType.Int32).Value = dto.Rating;

                        cmd.Parameters.Add("@comment", MySqlDbType.VarChar).Value =
                            string.IsNullOrWhiteSpace(dto.Comment) ? DBNull.Value : dto.Comment.Trim();

                        cmd.Parameters.Add("@score", MySqlDbType.Decimal).Value =
                            (object?)sentimentScore ?? DBNull.Value;

                        cmd.Parameters.Add("@label", MySqlDbType.VarChar).Value =
                            (object?)sentimentLabel ?? DBNull.Value;

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddReviewAsync: {ex}");
                throw;
            }
        }
    }
}