using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aufgabe2.Commands;
using Aufgabe2.Dtos;
using Aufgabe2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Aufgabe2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InventoryItemsController(InventoryService service) : ControllerBase
    {
        // TODO: Add your implementation
    }
}
