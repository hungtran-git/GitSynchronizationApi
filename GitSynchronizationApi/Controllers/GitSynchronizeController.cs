using GitSynchronizationApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GitSynchronizationApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GitSynchronizeController : ControllerBase
    {
        private readonly ILogger<GitSynchronizeController> _logger;
        private readonly IGitSynchronizeService _gitSynchronizeService;

        public GitSynchronizeController(ILogger<GitSynchronizeController> logger, IGitSynchronizeService gitSynchronizeService)
        {
            _logger = logger;
            _gitSynchronizeService = gitSynchronizeService;
        }

        [HttpPost]
        public async Task<JsonResult> Post([FromBody]SynchronizeModel synchronizeModel)
        {
            if (!ModelState.IsValid) {
                var errors = ModelState.SelectMany(_=>_.Value.Errors)
                                        .Select(_=>_.ErrorMessage)
                                        .Where(errorMsg=>!String.IsNullOrEmpty(errorMsg)).ToList();
                return new JsonResult(errors);
            }
            try
            {
                _logger.LogInformation("Starting...");
                /*
                                 var sourceUrl = "https://github.com/hungtran-git/RepoA.git";
                                var destinationUrl = "https://github.com/hungtran-git/RepoB.git";
                                var repoContainerPath = @"D:\HungTran\2-project\1-net-core\PowerShellHostedRunspaceStarterkits\TestRepoTranfer";
                 */
                var sourceUrl = synchronizeModel.SourceUrl;
                var destinationUrl = synchronizeModel.DestinationUrl;
                var repoContainerPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

                await _gitSynchronizeService.SyncRepository(sourceUrl, destinationUrl, repoContainerPath);
                _logger.LogInformation("Script execution completed");
                return new JsonResult(new { Message = "Sync completed" });
            }
            catch (Exception e)
            {
                return new JsonResult(new { Message = e.Message });
            }

        }
    }
}
