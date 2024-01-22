

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace woodgrove_portal.Controllers
{
    [ApiController]
    public class ProxyController : Controller
    {
        private readonly ILogger<ProxyController> _logger;
        private TelemetryClient _telemetry;
        private static readonly HttpClient _httpClient = new HttpClient();

        public ProxyController(ILogger<ProxyController> logger, TelemetryClient telemetry)
        {
            this._logger = logger;
            _telemetry = telemetry;
        }

        [Route("{**catchall}")]
        public IActionResult CatchAll(string catchAll = "")
        {
            return Content("{ \"name\":\"John Doe\", \"age\":31, \"city\":\"New York\" }", "application/json");
        }
    }
}
