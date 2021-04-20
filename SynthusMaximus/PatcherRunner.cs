using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SynthusMaximus.Patchers;
using SynthusMaximus.Support.RunSorting;
using Wabbajack.Common;

namespace SynthusMaximus
{
    public class PatcherRunner
    {
        private ILogger<PatcherRunner> _logger;
        private IEnumerable<IPatcher> _patchers;

        public PatcherRunner(ILogger<PatcherRunner> logger, IEnumerable<IPatcher> patchers)
        {
            _logger = logger;
            _patchers = patchers.SortByRunOrder();

        }

        public void RunPatchers()
        {
            _logger.LogInformation("Writing logs to {logfolder}", AbsolutePath.EntryPoint.Combine("logs"));
            AbsolutePath.EntryPoint.Combine("logs").CreateDirectory();
            foreach (var patcher in _patchers)
            {
                patcher.RunPatcher();
            }
        }
    }
}