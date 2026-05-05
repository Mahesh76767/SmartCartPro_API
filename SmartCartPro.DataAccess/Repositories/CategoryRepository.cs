using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using MySqlConnector;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SmartCartPro.DataAccess.Repositories
{
    public class CategoryRepository : BaseRepository, ICategoryRepository
    {
        public CategoryRepository(IConfiguration configuration) : base(configuration) { }

        public async Task<List<Category>> GetCategories()
        {
            try
            {
                var categoriesList = new List<Category>();

                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                    SELECT c.CategoryId, c.Name, c.Description, c.ParentCategoryId, 
                           p.Name AS ParentName, c.IsActive
                    FROM Categories c
                    LEFT JOIN Categories p ON c.ParentCategoryId = p.CategoryId
                    WHERE c.IsActive = 1
                    ORDER BY c.CategoryId DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categoriesList.Add(new Category
                            {
                                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"]?.ToString(),
                                ParentCategoryId = reader["ParentCategoryId"] == DBNull.Value
                                    ? null
                                    : (int?)Convert.ToInt32(reader["ParentCategoryId"]),
                                ParentName = reader["ParentName"]?.ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }

                return categoriesList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<Category?> GetCategoriesById(int Id)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT c.CategoryId, c.Name, c.Description, c.ParentCategoryId, 
                        p.Name AS ParentName, c.IsActive
                 FROM Categories c
                 LEFT JOIN Categories p ON c.ParentCategoryId = p.CategoryId
                 WHERE c.CategoryId = @Id";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Category
                                {
                                    CategoryId = reader.GetInt32("CategoryId"),
                                    Name = reader.GetString("Name"),
                                    Description = reader["Description"]?.ToString(),
                                    ParentCategoryId = reader["ParentCategoryId"] == DBNull.Value
                                        ? null
                                        : Convert.ToInt32(reader["ParentCategoryId"]),
                                    ParentName = reader["ParentName"]?.ToString(),
                                    IsActive = reader.GetBoolean("IsActive")
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    var query = excludeId.HasValue
                        ? "SELECT COUNT(*) FROM Categories WHERE Name = @name AND CategoryId <> @id"
                        : "SELECT COUNT(*) FROM Categories WHERE Name = @name";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
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

        public async Task<int>CreateAsync(Category category)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    var query = @"INSERT INTO Categories (Name, Description, ParentCategoryId, IsActive)
                        VALUES (@name, @desc, @parentId, 1);
                        SELECT LAST_INSERT_ID()";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", category.Name);
                        cmd.Parameters.AddWithValue("@desc", (object?)category.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@parentId", (object?)category.ParentCategoryId ?? DBNull.Value);
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> UpdateAsync(Category category)
        {
            try
            {
                using(MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE Categories SET Name = @name, Description = @desc,
                            ParentCategoryId = @parentId, IsActive = @active
                        WHERE CategoryId = @id";

                    using(MySqlCommand cmd = new MySqlCommand( query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", category.CategoryId);
                        cmd.Parameters.AddWithValue("@name", category.Name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@desc", category.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@parentId", category.ParentCategoryId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@active", category.IsActive);

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

        public async Task<bool> HasProductsAsync(int categoryId)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT COUNT(*) FROM Products WHERE CategoryId = @id AND IsActive = 1";

                    using(MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", categoryId);

                        var result = await cmd.ExecuteScalarAsync();
                        int count = result != null ? Convert.ToInt32(result) : 0;
                        return count > 0;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> SoftDeleteAsync(int categoryId)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE Categories SET IsActive = 0 WHERE CategoryId = @id ";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", categoryId);

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
    }
}
