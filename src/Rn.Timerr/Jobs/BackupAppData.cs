using Rn.Timerr.Enums;
using Rn.Timerr.Factories;
using Rn.Timerr.Models;

namespace Rn.Timerr.Jobs;

class BackupAppData : IRunnableJob
{
  public string Name => nameof(BackupAppData);
  public string ConfigKey => nameof(BackupAppData);

  private readonly ISshClientFactory _sshClientFactory;

  public BackupAppData(ISshClientFactory sshClientFactory)
  {
    _sshClientFactory = sshClientFactory;
  }


  // Interface methods
  public async Task<RunningJobResult> RunAsync(RunningJobOptions options)
  {
    var jobOutcome = new RunningJobResult(JobOutcome.Failed);
    var config = MapConfiguration(options);

    if (!config.IsValid())
      return jobOutcome.WithError("Missing required configuration");

    var sshClient = await _sshClientFactory.GetSshClient(config.SshCredsName);

    foreach (var folder in config.Folders)
    {
      var directory = Path.GetFileName(folder);
      var destPath = GenerateBackupDestPath(config, directory);

      sshClient.RunCommand($"mkdir -p \"{destPath}\"");
      sshClient.RunCommand($"rm \"{destPath}$(date '+%F')-{directory}.zip\"", false);
      sshClient.RunCommand($"zip -r \"{destPath}$(date '+%F')-{directory}.zip\" \"{folder}\"");
      sshClient.RunCommand($"chmod 0777 \"{destPath}$(date '+%F')-{directory}.zip\"");
    }

    options.ScheduleNextRunUsingTemplate(DateTime.Now.AddDays(1), "yyyy-MM-ddT08:20:00.0000000-07:00");
    return jobOutcome.AsSucceeded();
  }


  // Internal methods
  private static BackupAppDataConfig MapConfiguration(RunningJobOptions options) =>
    new()
    {
      Folders = options.Config.GetStringCollection("directory"),
      BackupDestRoot = options.Config.GetStringValue("backupDestRoot"),
      SshCredsName = options.Config.GetStringValue("ssh.creds")
    };

  private static string GenerateBackupDestPath(BackupAppDataConfig config, string directory)
  {
    var generated = Path.Join(config.BackupDestRoot, directory)
      .Replace("\\", "/");

    if (!generated.EndsWith('/'))
      generated += "/";

    return generated;
  }
}

class BackupAppDataConfig
{
  public List<string> Folders { get; set; } = new();
  public string BackupDestRoot { get; set; } = string.Empty;
  public string SshCredsName { get; set; } = string.Empty;

  public bool IsValid()
  {
    if (string.IsNullOrWhiteSpace(BackupDestRoot))
      return false;

    if (string.IsNullOrWhiteSpace(SshCredsName))
      return false;

    if (Folders.Count == 0)
      return false;

    return true;
  }
}
