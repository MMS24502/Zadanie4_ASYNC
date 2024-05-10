using Microsoft.AspNetCore.Mvc;
using Zadanie4_ASYNC.Models;
using Zadanie4_ASYNC.Repositories;

namespace Zadanie4_ASYNC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseController(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    // SQL
    [HttpPost("AddProductDirect")]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseDTO request)
    {
        var result = await _warehouseRepository.AddProductToWarehouse(request);
        if (!result)
            return BadRequest("Unable to add product to warehouse using direct method.");

        return Ok("Product added to warehouse using direct method.");
    }

    // Procedure
    [HttpPost("AddProductStoredProc")]
    public async Task<IActionResult> AddProductToWarehouseViaStoredProc([FromBody] ProductWarehouseDTO request)
    {
        var result = await _warehouseRepository.AddProductToWarehouseViaStoredProc(request);
        if (!result)
            return BadRequest("Unable to add product to warehouse using stored procedure.");

        return Ok("Product added to warehouse using stored procedure.");
    }
}
