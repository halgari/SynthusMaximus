using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SynthusMaximus.Patchers;
using SynthusMaximus.Support.RunSorting;

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
            foreach (var patcher in _patchers)
            {
                _logger.LogInformation("Running {Patcher}", patcher.GetType().Name);
                Stopwatch sw = Stopwatch.StartNew();
                patcher.RunPatcher();
                _logger.LogInformation("Finished running {Patcher} in {MS}ms", patcher.GetType().Name,
                    sw.ElapsedMilliseconds);

            }
        }
    }
}