using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Order;
using SmartCartPro.Models.Entities;
using SmartCartPro.Models.Enums;
using System.Diagnostics;

namespace SmartCartPro.DataAccess.Repositories
{
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        public OrderRepository(IConfiguration config) : base(config) { }

        public async Task<PagedResult<OrderResponseDto>> GetAllAsync(OrderFilterDto filter)
        {
            try
            {
                using(var con = GetConnection())
                {
                    await con.OpenAsync();

                    var where = new List<string>();

                    if (!string.IsNullOrWhiteSpace(filter.Status))
                        where.Add("o.Status = @status");

                    if (filter.DateFrom.HasValue)
                        where.Add("o.OrderDate >= @from");

                    if (filter.DateTo.HasValue)
                        where.Add("o.OrderDate <= @to");

                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                        where.Add("(c.FirstName LIKE @s OR c.LastName LIKE @s)");

                    string whereClause = where.Count > 0 ? $"WHERE {string.Join(" AND ", where)}":"";

                    string countSql = $@"
                    SELECT COUNT(*)
                    FROM Orders o
                    JOIN Customers c ON o.CustomerId = c.CustomerId
                    {whereClause}";

                    string dataSql = $@"
                        SELECT o.*,
                               CONCAT(c.FirstName,' ',c.LastName) AS CustomerName,
                               COUNT(oi.OrderItemId) AS ItemCount
                        FROM Orders o
                        JOIN Customers c ON o.CustomerId = c.CustomerId
                        LEFT JOIN OrderItems oi ON o.OrderId = oi.OrderId
                        {whereClause}
                        GROUP BY o.OrderId
                        ORDER BY o.OrderDate DESC
                        LIMIT @pageSize OFFSET @offset";

                    int total = 0;

                    using(MySqlCommand countCmd = new MySqlCommand(countSql, con))
                    {
                        if (!string.IsNullOrWhiteSpace(filter.Status))
                            countCmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = filter.Status;

                        if (filter.DateFrom.HasValue)
                            countCmd.Parameters.Add("@from", MySqlDbType.DateTime).Value = filter.DateFrom.Value;

                        if (filter.DateTo.HasValue)
                            countCmd.Parameters.Add("@to", MySqlDbType.DateTime).Value = filter.DateTo.Value;

                        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                            countCmd.Parameters.Add("@s", MySqlDbType.VarChar).Value = $"%{filter.SearchTerm}%";

                        var result = await countCmd.ExecuteScalarAsync();

                        total = Convert.ToInt32(result);
                    }

                    var list = new List<OrderResponseDto>();

                    using (MySqlCommand cmd = new MySqlCommand(dataSql, con))
                    {
                        if (!string.IsNullOrWhiteSpace(filter.Status))
                            cmd.Parameters.Add("@status", MySqlDbType.VarChar).Value = filter.Status;

                        if (filter.DateFrom.HasValue)
                            cmd.Parameters.Add("@from", MySqlDbType.DateTime).Value = filter.DateFrom.Value;

                        if (filter.DateTo.HasValue)
                            cmd.Parameters.Add("@to", MySqlDbType.DateTime).Value = filter.DateTo.Value;

                        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                            cmd.Parameters.Add("@s", MySqlDbType.VarChar).Value = $"%{filter.SearchTerm}%";

                        cmd.Parameters.Add("@pageSize", MySqlDbType.Int32).Value = filter.PageSize;
                        cmd.Parameters.Add("@offset", MySqlDbType.Int32).Value = filter.Offset;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(MapOrderSummary(reader));
                            }                    
                        }

                        return new PagedResult<OrderResponseDto>
                        {
                            Data = list,
                            TotalCount = total,
                            Page = filter.Page,
                            PageSize = filter.PageSize
                        };

                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<OrderResponseDto?> GetByIdAsync(int id)
        {
            try 
            {
                using (var con = GetConnection())
                {
                    await con.OpenAsync();

                    string orderSql = @"
                    SELECT o.*,
                           CONCAT(c.FirstName,' ',c.LastName) AS CustomerName,
                           0 AS ItemCount
                    FROM Orders o
                    JOIN Customers c ON o.CustomerId = c.CustomerId
                    WHERE o.OrderId = @id";

                    OrderResponseDto? order = null;

                    using (MySqlCommand cmd = new MySqlCommand(orderSql, con))
                    {
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return null;
                            }

                            order = MapOrderSummary(reader);
                        }
                    }

                    string itemSql = @"
                    SELECT oi.*, 
                           p.Name AS ProductName, 
                           p.SKU, 
                           p.ImageUrl
                    FROM OrderItems oi
                    JOIN Products p ON oi.ProductId = p.ProductId
                    WHERE oi.OrderId = @id";

                    using (MySqlCommand itemCmd = new MySqlCommand(itemSql, con))
                    {
                        itemCmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                        using (var reader = await itemCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                order!.Items.Add(new OrderItemResponseDto
                                {
                                    OrderItemId = reader.GetInt32("OrderItemId"),
                                    ProductId = reader.GetInt32("ProductId"),
                                    ProductName = reader.GetString("ProductName"),
                                    SKU = reader.GetString("SKU"),
                                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                                                    ? null
                                                    : reader.GetString("ImageUrl"),
                                    Quantity = reader.GetInt32("Quantity"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    TotalPrice = reader.GetDecimal("TotalPrice")
                                });
                            }
                        }
                    }

                    order!.ItemCount = order.Items.Count;

                    return order;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex}");
                throw;
            }
        }

        public async Task<int> CreateAsync(CreateOrderDto dto, decimal totalAmount, decimal discountAmount, int? discountCodeId)
        {
            using (MySqlConnection conn = GetConnection())
            {
                await conn.OpenAsync();

                using (var tran = await conn.BeginTransactionAsync())
                {
                    try
                    {
                        // 🔹 1. Insert Order
                        string orderSql = @"
                        INSERT INTO Orders
                        (CustomerId, OrderDate, Status, TotalAmount, DiscountAmount, ShippingAddress, Notes, DiscountCodeId, CreatedAt)
                        VALUES (@cid, NOW(), 'Pending', @total, @disc, @addr, @notes, @codeId, NOW());
                        SELECT LAST_INSERT_ID();";

                        int orderId;

                        using (MySqlCommand oc = new MySqlCommand(orderSql, conn, (MySqlTransaction)tran))
                        {
                            oc.Parameters.Add("@cid", MySqlDbType.Int32).Value = dto.CustomerId;
                            oc.Parameters.Add("@total", MySqlDbType.Decimal).Value = totalAmount;
                            oc.Parameters.Add("@disc", MySqlDbType.Decimal).Value = discountAmount;
                            oc.Parameters.Add("@addr", MySqlDbType.VarChar).Value = dto.ShippingAddress;
                            oc.Parameters.Add("@notes", MySqlDbType.VarChar).Value = (object?)dto.Notes ?? DBNull.Value;
                            oc.Parameters.Add("@codeId", MySqlDbType.Int32).Value = (object?)discountCodeId ?? DBNull.Value;

                            orderId = Convert.ToInt32(await oc.ExecuteScalarAsync());
                        }

                        // 🔹 2. Insert OrderItems + Update Inventory
                        foreach (var item in dto.Items)
                        {
                            decimal price;

                            // Get product price
                            using (MySqlCommand pc = new MySqlCommand(
                                "SELECT Price FROM Products WHERE ProductId=@pid", conn, (MySqlTransaction)tran))
                            {
                                pc.Parameters.Add("@pid", MySqlDbType.Int32).Value = item.ProductId;

                                var result = await pc.ExecuteScalarAsync();
                                if (result == null)
                                    throw new Exception($"Product {item.ProductId} not found");

                                price = Convert.ToDecimal(result);
                            }

                            // Insert Order Item
                            using (MySqlCommand ic = new MySqlCommand(@"
                                INSERT INTO OrderItems 
                                (OrderId, ProductId, Quantity, UnitPrice, TotalPrice) 
                                VALUES (@oid, @pid, @qty, @up, @tp)",
                                conn, (MySqlTransaction)tran))
                            {
                                ic.Parameters.Add("@oid", MySqlDbType.Int32).Value = orderId;
                                ic.Parameters.Add("@pid", MySqlDbType.Int32).Value = item.ProductId;
                                ic.Parameters.Add("@qty", MySqlDbType.Int32).Value = item.Quantity;
                                ic.Parameters.Add("@up", MySqlDbType.Decimal).Value = price;
                                ic.Parameters.Add("@tp", MySqlDbType.Decimal).Value = price * item.Quantity;

                                await ic.ExecuteNonQueryAsync();
                            }

                            // Update Inventory
                            using (MySqlCommand inv = new MySqlCommand(@"
                                UPDATE Inventory 
                                SET CurrentStock = CurrentStock - @qty,
                                    LastUpdated = NOW()
                                WHERE ProductId = @pid",
                                conn, (MySqlTransaction)tran))
                            {
                                inv.Parameters.Add("@qty", MySqlDbType.Int32).Value = item.Quantity;
                                inv.Parameters.Add("@pid", MySqlDbType.Int32).Value = item.ProductId;

                                int affected = await inv.ExecuteNonQueryAsync();
                                if (affected == 0)
                                    throw new Exception($"Inventory update failed for Product {item.ProductId}");
                            }
                        }

                        // 🔹 3. Update Discount Usage
                        if (discountCodeId.HasValue)
                        {
                            using (MySqlCommand dc = new MySqlCommand(@"
                                UPDATE DiscountCodes 
                                SET UsedCount = UsedCount + 1 
                                WHERE CodeId = @id",
                                conn, (MySqlTransaction)tran))
                            {
                                dc.Parameters.Add("@id", MySqlDbType.Int32).Value = discountCodeId.Value;
                                await dc.ExecuteNonQueryAsync();
                            }
                        }

                        // 🔹 Commit Transaction
                        await tran.CommitAsync();

                        return orderId;
                    }
                    catch (Exception ex)
                    {
                        await tran.RollbackAsync();
                        Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            try
            {
                using(var con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"UPDATE Orders SET STATUS = @status WHERE OrderId = @id";

                    using(var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@status", MySqlDbType.VarChar).Value = status;
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
                        int result = await cmd.ExecuteNonQueryAsync();

                        return result > 0;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in UpdateStatusAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CancelAsync(int id, string reason)
        {
            using (MySqlConnection conn = GetConnection())
            {
                await conn.OpenAsync();

                using (var tran = await conn.BeginTransactionAsync())
                {
                    try
                    {
                        // 🔹 1. Check order exists & not already cancelled
                        var checkSql = "SELECT Status FROM Orders WHERE OrderId = @id";

                        string? currentStatus = null;

                        using (var checkCmd = new MySqlCommand(checkSql, conn, (MySqlTransaction)tran))
                        {
                            checkCmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                            var result = await checkCmd.ExecuteScalarAsync();

                            if(result == null)
                            {
                                throw new Exception($"Order {id} not found");
                            }

                            currentStatus = result.ToString();
                        }

                        if (currentStatus == "Cancelled")
                            return true;

                        // 🔹 2. Update order status

                        string updateSql = @"UPDATE Orders
                            SET Status = 'Cancelled',
                                CreatedAt = NOW()
                            WHERE OrderId = @id";

                        using(var uc = new MySqlCommand(updateSql, conn, (MySqlTransaction)tran))
                        {
                            uc.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                            var affected = await uc.ExecuteNonQueryAsync();
                            if(affected == 0)
                            {
                                throw new Exception("Failed to update order status");
                            }
                        }

                        // 🔹 3. Get order items

                        var item = new List<(int pid, int qty)>();

                        string itemSql = @"SELECT ProductId, Quantity
                                            FROM OrderItems
                                            WHERE OrderId = @id";

                        using (var ic = new MySqlCommand(itemSql, conn, (MySqlTransaction)tran))
                        {
                            ic.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

                            using(var reader = await ic.ExecuteReaderAsync())
                            {
                                while(await reader.ReadAsync())
                                {
                                    item.Add((
                                        reader.GetInt32("ProductId"),
                                        reader.GetInt32("Quantity")
                                    ));
                                }
                            }
                        }

                        // 🔹 4. Restore inventory + log movement

                        foreach(var (pid, qty) in item)
                        {
                            // Restore stock
                            string restoreSql = @"UPDATE Inventory
                            SET CurrentStock = CurrentStock + @qty,
                                LastUpdated = NOW()
                            WHERE ProductId = @pid";

                            using (var rc = new MySqlCommand(restoreSql, conn, (MySqlTransaction)tran))
                            {
                                rc.Parameters.Add("@qty", MySqlDbType.Int32).Value = qty;
                                rc.Parameters.Add("@pid", MySqlDbType.Int32).Value = pid;

                                await rc.ExecuteNonQueryAsync();
                            }

                            // Log stock movement

                            string logstockSql = @"INSERT INTO StockMovements
                            (ProductId, MovementType, Quantity, Reason, MovedBy, MovedAt)
                            VALUES (@pid, 'Return', @qty, @reason, @user, NOW())";

                            using(var mc = new MySqlCommand(logstockSql, conn, (MySqlTransaction)tran))
                            {
                                mc.Parameters.Add("@pid", MySqlDbType.Int32).Value = pid;
                                mc.Parameters.Add("@qty", MySqlDbType.Int32).Value = qty;
                                mc.Parameters.Add("@reason", MySqlDbType.VarChar)
                                  .Value = $"Order #{id} cancelled: {reason}";
                                mc.Parameters.Add("@user", MySqlDbType.Int32).Value = 1; // TODO: replace with logged-in user

                                await mc.ExecuteNonQueryAsync();
                            }
                        }

                        // 🔹 Commit

                        await tran.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await tran.RollbackAsync();
                        Console.WriteLine($"Error in CancelAsync: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public async Task<DiscountResponseDto?> ValidateDiscountAsync(string code)
        {
            using (MySqlConnection conn = GetConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                SELECT CodeId, Code, DiscountPercent
                FROM DiscountCodes
                WHERE Code = @code
                  AND IsActive = 1
                  AND Expiry > NOW()
                  AND UsedCount < MaxUses
                LIMIT 1";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@code", MySqlDbType.VarChar)
                       .Value = code.ToUpper().Trim();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            return null;

                        decimal percent = reader.GetDecimal("DiscountPercent");

                        return new DiscountResponseDto
                        {
                            CodeId = reader.GetInt32("CodeId"),
                            Code = reader.GetString("Code"),
                            DiscountPercent = percent,
                            Message = $"{percent}% discount applied!"
                        };
                    }
                }
            }
        }

        private static OrderResponseDto MapOrderSummary(MySqlDataReader r) => new()
        {
            OrderId = r.GetInt32("OrderId"),
            CustomerId = r.GetInt32("CustomerId"),
            CustomerName = r.IsDBNull(r.GetOrdinal("CustomerName")) ? "" : r.GetString("CustomerName"),
            OrderDate = r.GetDateTime("OrderDate"),
            Status = r.GetString("Status"),
            TotalAmount = r.GetDecimal("TotalAmount"),
            DiscountAmount = r.GetDecimal("DiscountAmount"),
            ShippingAddress = r.GetString("ShippingAddress"),
            Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString("Notes"),
            ItemCount = Convert.ToInt32(r["ItemCount"])
        };

        public async Task<(bool exists, decimal price, int stock, string name)> GetProductInfoAsync(int productId)
        {
            try
            {
                using (MySqlConnection con = GetConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                    SELECT p.Price, p.Name, COALESCE(i.CurrentStock, 0) AS Stock
                    FROM Products p
                    LEFT JOIN Inventory i ON p.ProductId = i.ProductId
                    WHERE p.ProductId = @id AND p.IsActive = 1
                    LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = productId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                                return (false, 0, 0, "");

                            return (
                                true,
                                reader.GetDecimal("Price"),
                                reader.GetInt32("Stock"),
                                reader.IsDBNull(reader.GetOrdinal("Name"))
                                    ? ""
                                    : reader.GetString("Name")
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetProductInfoAsync: {ex}");
                throw;
            }
        }
    }
}