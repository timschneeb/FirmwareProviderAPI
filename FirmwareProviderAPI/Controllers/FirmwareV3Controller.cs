using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FirmwareProviderAPI.Controllers
{
    [ApiController]
    [Route("v3")]
    public class FirmwareV3Controller : ControllerBase
    {
        private readonly ILogger<FirmwareController> _logger;
        
        public FirmwareV3Controller(ILogger<FirmwareController> logger)
        {
            _logger = logger;
        }

        // GET: /v3/firmware
        [HttpGet]
        [Route("/v3/firmware")]
        public async Task<ActionResult<IEnumerable<Firmware>>> GetAll()
        {
            return FirmwareIndexer.Firmwares.ToList();
        }
        
        // GET: /v3/firmware/buds
        [HttpGet]
        [Route("/v3/firmware/{model}")]
        public async Task<ActionResult<IEnumerable<Firmware>>> Get(Models model)
        {
            return FirmwareIndexer.Firmwares.Where(x => x.Model == model).ToList();
        }
        
        // GET: /v3/firmware/download/r175xxu0atf2
        [HttpGet("/v3/firmware/download/{build}")]
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