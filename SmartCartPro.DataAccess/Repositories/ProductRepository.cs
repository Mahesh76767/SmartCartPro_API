using MySqlConnector;
using Microsoft.Extensions.Configuration;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Entities;
using SmartCartPro.Models.DTOs.Product;
using SmartCartPro.Models.Common;

namespace SmartCartPro.DataAccess.Repositories
{
    public class ProductRepository : BaseRepository, IProductRepository
    {
        public ProductRepository(IConfiguration config) : base(config) { }

        public async Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductFilterDto filter)
        {
            // ? Ensure valid pagination
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            var where = "WHERE p.IsActive = 1";
            var parameters = new Dictionary<string, object?>();

            // ? Filtering
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                where += " AND (p.Name LIKE @search OR p.SKU LIKE @search)";
                parameters["@search"] = $"%{filter.SearchTerm}%";
            }

            if (filter.CategoryId.HasValue)
            {
                where += " AND p.CategoryId = @categoryId";
                parameters["@categoryId"] = filter.CategoryId;
            }

            if (filter.MinPrice.HasValue)
            {
                where += " AND p.Price >= @minPrice";
                parameters["@minPrice"] = filter.MinPrice;
            }

            if (filter.MaxPrice.HasValue)
            {
                where += " AND p.Price <= @maxPrice";
                parameters["@maxPrice"] = filter.MaxPrice;
            }

            // ? SAFE SORTING (Prevent SQL injection + invalid columns)
            var allowedSortColumns = new[] { "ProductId", "Name", "Price", "CreatedAt" };
            var sortBy = allowedSortColumns.Contains(filter.SortBy) ? filter.SortBy : "ProductId";
            var sortDir = filter.SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";

            var countSql = $"SELECT COUNT(*) FROM Products p {where}";

            var dataSql = $@"
        SELECT 
            p.ProductId, p.SKU, p.Name, p.Description, p.Price,
            p.CategoryId, p.ImageUrl, p.IsActive,
            p.CreatedAt, p.UpdatedAt,
            c.Name AS CategoryName,
            COALESCE(i.CurrentStock, 0) AS CurrentStock,
            COALESCE(i.MinStockLevel, 5) AS MinStockLevel
        FROM Products p
        LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
        LEFT JOIN Inventory i ON p.ProductId = i.ProductId
        {where}
        ORDER BY p.{sortBy} {sortDir}
        LIMIT @pageSize OFFSET @offset";

            var countParams = new Dictionary<string, object?>(parameters);

            parameters["@pageSize"] = filter.PageSize;
            parameters["@offset"] = (filter.Page - 1) * filter.PageSize;

            using var conn = GetConnection();
            await conn.OpenAsync();

            // ? COUNT
            using var countCmd = new MySqlCommand(countSql, conn);
            foreach (var p in countParams)
                countCmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

            var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // ? DATA
            using var cmd = new MySqlCommand(dataSql, conn);
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

            var products = new List<ProductResponseDto>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return new PagedResult<ProductResponseDto>
            {
                Data = products,
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }


        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT
                            p.ProductId, p.SKU, p.Name, p.Description, p.Price,
                            p.CategoryId, p.ImageUrl, p.IsActive,
                            p.CreatedAt, p.UpdatedAt,
                            c.Name AS CategoryName,
                            COALESCE(i.CurrentStock, 0)  AS CurrentStock,
                            COALESCE(i.MinStockLevel, 5)  AS MinStockLevel
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
                        LEFT JOIN Inventory  i ON p.ProductId  = i.ProductId
                        WHERE p.ProductId = @id";

                    using var cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", id);
                    using var r = await cmd.ExecuteReaderAsync();
                    return await r.ReadAsync() ? MapProduct(r) : null;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<int> CreateAsync(CreateProductDto dto)
        {
            using (MySqlConnection con = GetConnection())
            {
                await con.OpenAsync();

                using (var tran = await con.BeginTransactionAsync())
                {
                    try
                    {
                        //1. Insert Product
                        string productSql = @"INSERT INTO Products
                            (SKU, Name, Description, Price, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt)
                        VALUES
                            (@sku, @name, @desc, @price, @catId, @img, 1, NOW(), NOW());
                        SELECT LAST_INSERT_ID();";

                        int productId;

                        using (var cmd = new MySqlCommand(productSql, con, (MySqlTransaction)tran))
                        {
                            cmd.Parameters.AddWithValue("@sku", dto.SKU);
                            cmd.Parameters.AddWithValue("@name", dto.Name);
                            cmd.Parameters.AddWithValue("@desc", (object?)dto.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@price", dto.Price);
                            cmd.Parameters.AddWithValue("@catId", dto.CategoryId);
                            cmd.Parameters.AddWithValue("@img", (object?)dto.ImageUrl ?? DBNull.Value);

                            productId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }

                        // 2. Insert Inventory
                        string invSql = @"INSERT INTO Inventory
                            (ProductId, CurrentStock, MinStockLevel, MaxStockLevel, LastUpdated)
                        VALUES
                            (@pid, @stock, @minStock, @maxStock, NOW());";

                        using (var iCmd = new MySqlCommand(invSql, con, (MySqlTransaction)tran))
                        {
                            iCmd.Parameters.AddWithValue("@pid", productId);
                            iCmd.Parameters.AddWithValue("@stock", dto.InitialStock);
                            iCmd.Parameters.AddWithValue("@minStock", dto.MinStockLevel);
                            iCmd.Parameters.AddWithValue("@maxStock", dto.MaxStockLevel);

                            await iCmd.ExecuteNonQueryAsync();
                        }

                        //3. Commit Transaction
                        await tran.CommitAsync();

                        return productId;
                    }
                    catch (Exception ex)
                    {
                        await tran.RollbackAsync();

                        Console.WriteLine($"Error Occurred: {ex.Message}");
                        throw;
                    }
                }
            }
        }


        public async Task<bool> SKUExistsAsync(string sku, int? excludeId = null)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = excludeId.HasValue
                    ? "SELECT COUNT(*) FROM Products WHERE SKU = @sku AND ProductId <> @id"
                    : "SELECT COUNT(*) FROM Products WHERE SKU = @sku";


                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sku", sku);
                        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@id", excludeId.Value);

                        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> CategoryExistsAsync(int categoryId)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT COUNT(*) FROM Categories WHERE CategoryId = @id AND IsActive = 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("id", categoryId);

                        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool>UpdateAsync(int id, UpdateProductDto dto)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    var sets = new List<string> { "UpdatedAt = NOW()" };
                    var parameters = new Dictionary<string, object?>();

                    if (dto.Name != null)
                    {
                        sets.Add("Name = @name");
                        parameters["@name"] = dto.Name;
                    }

                    if (dto.Description != null)
                    {
                        sets.Add("Description = @desc");
                        parameters["@desc"] = dto.Description;
                    }

                    if (dto.Price.HasValue)
                    {
                        sets.Add("Price = @price");
                        parameters["@price"] = dto.Price;
                    }

                    if (dto.CategoryId.HasValue)
                    {
                        sets.Add("CategoryId = @catId");
                        parameters["@catId"] = dto.CategoryId;
                    }

                    if (dto.ImageUrl != null)
                    {
                        sets.Add("ImageUrl = @img");
                        parameters["@img"] = dto.ImageUrl;
                    }

                    if (dto.IsActive.HasValue)
                    {
                        sets.Add("IsActive = @active");
                        parameters["@active"] = dto.IsActive;
                    }

                    parameters["@id"] = id;

                    string query = $"UPDATE Products SET {string.Join(", ", sets)} WHERE ProductId = @id";

                    int rows;

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

                        rows = await cmd.ExecuteNonQueryAsync();
                    }

                    if (dto.MinStockLevel.HasValue || dto.MaxStockLevel.HasValue)
                    {
                        var invSets = new List<string> { "LastUpdated = NOW()" };
                        var invParams = new Dictionary<string, object?>();

                        if (dto.MinStockLevel.HasValue)
                        {
                            invSets.Add("MinStockLevel = @min");
                            invParams["@min"] = dto.MinStockLevel.Value;
                        }

                        if (dto.MaxStockLevel.HasValue)
                        {
                            invSets.Add("MaxStockLevel = @max");
                            invParams["@max"] = dto.MaxStockLevel.Value;
                        }

                        invParams["@id"] = id;

                        string invQuery = $"UPDATE Inventory SET {string.Join(", ", invSets)} WHERE ProductId = @id";

                        using (MySqlCommand cmd = new MySqlCommand(invQuery, con))
                        {
                            foreach (var p in invParams)
                                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool>SoftDeleteAsync(int id)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE Products SET IsActive = 0, UpdatedAt = NOW() WHERE ProductId = @id";

                    using(MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        return await cmd.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        private static ProductResponseDto MapProduct(MySqlDataReader r) => new()
        {
            ProductId = r.GetInt32("ProductId"),
            SKU = r.GetString("SKU"),
            Name = r.GetString("Name"),
            Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString("Description"),
            Price = r.GetDecimal("Price"),
            CategoryId = r.GetInt32("CategoryId"),
            CategoryName = r.IsDBNull(r.GetOrdinal("CategoryName")) ? null : r.GetString("CategoryName"),
            ImageUrl = r.IsDBNull(r.GetOrdinal("ImageUrl")) ? null : r.GetString("ImageUrl"),
            CurrentStock = r.GetInt32("CurrentStock"),
            MinStockLevel = r.GetInt32("MinStockLevel"),
            IsActive = r.GetBoolean("IsActive"),
            CreatedAt = r.GetDateTime("CreatedAt"),
            UpdatedAt = r.GetDateTime("UpdatedAt")
        };

    }
}