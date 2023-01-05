using Rn.Timerr.Enums;
using Rn.Timerr.Extensions;
using Rn.Timerr.Jobs;
using Rn.Timerr.Models;
using Rn.Timerr.Models.Config;
using Rn.Timerr.Models.Entities;
using Rn.Timerr.Repos;
using RnCore.Logging;

namespace Rn.Timerr.Services;

interface IJobRunnerService
{
  Task RunJobsAsync();
}

class JobRunnerService : IJobRunnerService
{
  private readonly ILoggerAdapter<JobRunnerService> _logger;
  private readonly List<IRunnableJob> _jobs;
  private readonly IJobConfigService _jobConfigService;
  private readonly IJobStateService _jobStateService;
  private readonly IJobsRepo _jobsRepo;
  private readonly RnTimerrConfig _config;
  private readonly List<JobEntity> _enabledJobs = new();

  public JobRunnerService(
    ILoggerAdapter<JobRunnerService> logger,
    IEnumerable<IRunnableJob> runnableJobs,
    IJobConfigService jobConfigService,
    IJobStateService jobStateService,
    RnTimerrConfig config,
    IJobsRepo jobsRepo)
  {
    _jobConfigService = jobConfigService;
    _jobStateService = jobStateService;
    _config = config;
    _jobsRepo = jobsRepo;
    _logger = logger;
    _jobs = runnableJobs.ToList();
  }


  // Interface methods
  public async Task RunJobsAsync()
  {
    if (_jobs.Count == 0)
      return;

    await RefreshEnabledJobs();

    foreach (var job in _jobs)
    {
      // Skip over any disabled jobs
      if (!_enabledJobs.Any(x => x.JobName.IgnoreCaseEquals(job.ConfigKey)))
        continue;

      // Build up the job configuration
      var jobOptions = new RunningJobOptions(job.ConfigKey, _config.Host)
      {
        Config = await _jobConfigService.GetJobConfig(job.ConfigKey),
        State = await _jobStateService.GetJobStateAsync(job.ConfigKey),
        JobStartTime = DateTimeOffset.Now
      };

      if (!job.CanRun(jobOptions))
        continue;

      _logger.LogInformation("Running Job: {name}", job.Name);
      var jobResult = await job.RunAsync(jobOptions);

      if (jobResult.Outcome != JobOutcome.Succeeded)
      {
        _logger.LogWarning("Job {name} failed: {reason}", job.Name, jobResult.Error);
        return;
      }

      await _jobStateService.PersistStateAsync(jobOptions);
    }
  }


  // Internal methods
  private async Task RefreshEnabledJobs()
  {
    _logger.LogTrace("Refreshing enabled jobs");
    _enabledJobs.Clear();
    _enabledJobs.AddRange(await _jobsRepo.GetJobsAsync(_config.Host));
  }
}
