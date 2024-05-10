using System.Data;
using Microsoft.Data.SqlClient;
using Zadanie4_ASYNC.Models;

namespace Zadanie4_ASYNC.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> AddProductToWarehouse(ProductWarehouseDTO productWarehouseDto) {
        using var con = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await con.OpenAsync();

        // Walidacja requestu
        var productWarehouseQuery = @"
            SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct;
            SELECT Price FROM Product WHERE IdProduct = @IdProduct;
            SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse;";
        
        using var checkCmd = new SqlCommand(productWarehouseQuery, con);
        checkCmd.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
        checkCmd.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
        
        var reader = await checkCmd.ExecuteReaderAsync();

        // Czy produkt istnieje
        await reader.ReadAsync();
        var productExists = reader.GetInt32(0) > 0;
        decimal productPrice = 0;
        if (productExists)
        {
            await reader.NextResultAsync();
            await reader.ReadAsync();
            productPrice = reader.GetDecimal(0);
        }

        // Czy warehouse istnieje
        await reader.NextResultAsync();
        await reader.ReadAsync();
        var warehouseExists = reader.GetInt32(0) > 0;
        reader.Close();

        if (!productExists || !warehouseExists || productWarehouseDto.Amount <= 0)
            return false;

        // Walidacja zamowienia
        var orderQuery = @"
            SELECT IdOrder FROM [Order] 
            WHERE IdProduct = @IdProduct AND Amount >= @Amount AND CreatedAt <= @CreatedAt AND FulfilledAt IS NULL;";
        
        using var orderCmd = new SqlCommand(orderQuery, con);
        orderCmd.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
        orderCmd.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
        orderCmd.Parameters.AddWithValue("@CreatedAt", productWarehouseDto.CreatedAt);
        
        var orderId = (int?)await orderCmd.ExecuteScalarAsync();
        
        if (!orderId.HasValue)
            return false;

        // Aktualizacja ORDER
        var updateOrderQuery = "UPDATE [Order] SET FulfilledAt = @Now WHERE IdOrder = @IdOrder;";
        using var updateOrderCmd = new SqlCommand(updateOrderQuery, con);
        updateOrderCmd.Parameters.AddWithValue("@IdOrder", orderId.Value);
        updateOrderCmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        await updateOrderCmd.ExecuteNonQueryAsync();

        // Tworzenie nowego rekordu w Product_Warehouse
        var insertQuery = @"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) 
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);";

        using var insertCmd = new SqlCommand(insertQuery, con);
        insertCmd.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
        insertCmd.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
        insertCmd.Parameters.AddWithValue("@IdOrder", orderId.Value);
        insertCmd.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
        insertCmd.Parameters.AddWithValue("@Price", productWarehouseDto.Amount * productPrice); // Correctly using fetched product price
        insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        
        var affectedRows = await insertCmd.ExecuteNonQueryAsync();
        return affectedRows > 0;
    }
    
    public async Task<bool> AddProductToWarehouseViaStoredProc(ProductWarehouseDTO productWarehouseDto)
    {
        using var con = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await con.OpenAsync();

        using var cmd = new SqlCommand("AddProductToWarehouse", con)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
        cmd.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
        cmd.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", productWarehouseDto.CreatedAt);

        var result = await cmd.ExecuteNonQueryAsync();
        return result > 0;
    }

}