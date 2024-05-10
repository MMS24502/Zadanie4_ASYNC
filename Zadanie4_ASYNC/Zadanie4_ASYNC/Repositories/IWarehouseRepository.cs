using Zadanie4_ASYNC.Models;

namespace Zadanie4_ASYNC.Repositories;

public interface IWarehouseRepository
{
    Task<bool> AddProductToWarehouse(ProductWarehouseDTO productWarehouse);
    Task<bool> AddProductToWarehouseViaStoredProc(ProductWarehouseDTO productWarehouseDto);
}