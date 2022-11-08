using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rn.Timerr.Services;
using RnCore.Abstractions;
using RnCore.Logging;
using System.Reflection;
using Rn.Timerr.Jobs;
using Rn.Timerr.Providers;
using Rn.Timerr.Helpers;

namespace Rn.Timerr.Extensions;

static class ServiceCollectionExtensions
{
  public static IServiceCollection AddRnTimerr(this IServiceCollection services)
  {
    // Logging
    services.TryAddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

    return services
      // Abstractions
      .AddSingleton<IDirectoryAbstraction, DirectoryAbstraction>()
      .AddSingleton<IFileAbstraction, FileAbstraction>()
      .AddSingleton<IDateTimeAbstraction, DateTimeAbstraction>()
      .AddSingleton<IPathAbstraction, PathAbstraction>()

      // Services
      .AddSingleton<IJobRunnerService, JobRunnerService>()

      // Providers
      .AddSingleton<IJobConfigProvider, JobConfigProvider>()

      // Helpers
      .AddSingleton<IJsonHelper, JsonHelper>()
      
      // Register runnable jobs
      .RegisterImplementations(Assembly.GetExecutingAssembly(), typeof(IRunnableJob));
  }
  
  private static IServiceCollection RegisterImplementations(this IServiceCollection me, Assembly? assembly, Type targetType)
  {
    if (assembly is null)
      throw new ArgumentNullException(nameof(assembly));

    var implementors = assembly
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(targetType))
      .ToList();

    foreach (Type? implType in implementors)
      me.AddSingleton(targetType, implType);

    return me;
  }
}
