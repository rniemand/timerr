using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rn.Timerr.Services;
using RnCore.Abstractions;
using RnCore.Logging;
using System.Reflection;
using Rn.Timerr.Jobs;
using Microsoft.Extensions.Configuration;
using Rn.Timerr.Models.Config;
using Rn.Timerr.Repos;
using RnCore.Mailer;

namespace Rn.Timerr.Extensions;

static class ServiceCollectionExtensions
{
  public static IServiceCollection AddRnTimerr(this IServiceCollection services, IConfiguration configuration)
  {
    // Logging
    services.TryAddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

    return services
      .AddSingleton(GetRnTimerrConfig(configuration))

      // Other libs
      .AddRnMailer(configuration)

      // Abstractions
      .AddSingleton<IDirectoryAbstraction, DirectoryAbstraction>()
      .AddSingleton<IFileAbstraction, FileAbstraction>()
      .AddSingleton<IDateTimeAbstraction, DateTimeAbstraction>()
      .AddSingleton<IPathAbstraction, PathAbstraction>()

      // Services
      .AddSingleton<IJobRunnerService, JobRunnerService>()
      .AddSingleton<IJobConfigService, JobConfigService>()
      .AddSingleton<IJobStateService, JobStateService>()
      
      // Database
      .AddSingleton<IConnectionFactory, ConnectionFactory>()
      .AddSingleton<IConfigRepo, ConfigRepo>()
      .AddSingleton<IStateRepo, StateRepo>()
      .AddSingleton<IJobsRepo, JobsRepo>()
      
      // Register runnable jobs
      .RegisterImplementations(Assembly.GetExecutingAssembly(), typeof(IRunnableJob));
  }
  
  private static IServiceCollection RegisterImplementations(this IServiceCollection me, Assembly? assembly, Type targetType)
  {
    if (assembly is null)
      throw new ArgumentNullException(nameof(assembly));

    var implementors = assembly
      .GetTypes()
      .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(targetType))
      .ToList();

    foreach (Type? implType in implementors)
      me.AddSingleton(targetType, implType);

    return me;
  }

  private static RnTimerrConfig GetRnTimerrConfig(IConfiguration config)
  {
    var boundConfig = new RnTimerrConfig();
    var section = config.GetSection("RnTimerr");

    if (section.Exists())
      section.Bind(boundConfig);

    return boundConfig;
  }
}
