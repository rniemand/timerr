﻿using Rn.Timerr.Models;

namespace Rn.Timerr.Jobs;

interface IRunnableJob
{
  string Name { get; }
  string ConfigKey { get; }

  bool CanRun(DateTime currentTime);

  Task<JobOutcome> RunAsync(JobOptions jobConfig);
}
