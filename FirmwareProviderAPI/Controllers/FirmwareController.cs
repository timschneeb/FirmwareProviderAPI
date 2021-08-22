using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FirmwareProviderAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FirmwareController : ControllerBase
    {
        private readonly ILogger<FirmwareController> _logger;
        
        public FirmwareController(ILogger<FirmwareController> logger)
        {
            _logger = logger;
        }

        // GET: /firmware
        [HttpGet]
        [Route("/[controller]")]
        public async Task<ActionResult<IEnumerable<Firmware>>> GetAll()
        {
            return FirmwareIndexer.Firmwares.ToList();
        }
        
        // GET: /firmware/buds
        [HttpGet]
        [Route("/[controller]/{model}")]
        public async Task<ActionResult<IEnumerable<Firmware>>> Get(Models model)
        {
            return FirmwareIndexer.Firmwares.Where(x => x.Model == model).ToList();
        }
        
        // GET: /firmware/download/r175xxu0atf2
        [HttpGet("/[controller]/download/{build}")]
        public async Task<ActionResult> Download(string build)
        {
            try
            {
                var fw = FirmwareIndexer.Firmwares.First(x => x.BuildName == build);
                return new FileContentResult(await System.IO.File.ReadAllBytesAsync(fw.Path), "application/octet-stream");
            }
            catch (Exception)
            {
                return new NotFoundResult();
            }
        }
    }
}