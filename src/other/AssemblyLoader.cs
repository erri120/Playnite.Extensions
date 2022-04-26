using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Other;

internal static class AssemblyLoader
{
    public static void ValidateReferencedAssemblies<T>(ILogger<T> logger)
    {
        var currentDomain = AppDomain.CurrentDomain;
        var loadedAssemblies = currentDomain.GetAssemblies();

        var currentAssembly = typeof(AssemblyLoader).Assembly;
        var currentAssemblyFolder = Path.GetDirectoryName(currentAssembly.Location)!;

        var referencedAssemblyNames = currentAssembly.GetReferencedAssemblies();

        foreach (var referencedAssemblyName in referencedAssemblyNames)
        {
            var loadedAssembly = loadedAssemblies.FirstOrDefault(x => x.GetName().Name.Equals(referencedAssemblyName.Name));

            // the referenced Assembly was already loaded so no need to do anything
            if (loadedAssembly is not null)
            {
                ValidateAssemblyVersions(logger, referencedAssemblyName, loadedAssembly.GetName());
                continue;
            }

            // the DLL should be in the same folder so let's look for that
            var dllPath = Path.Combine(currentAssemblyFolder, $"{referencedAssemblyName.Name}.dll");
            if (File.Exists(dllPath))
            {
                try
                {
                    loadedAssembly = Assembly.LoadFrom(dllPath);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unable to load Assembly {AssemblyFullName} from {Path}", referencedAssemblyName.FullName, dllPath);
                    loadedAssembly = null;
                }

                // we successfully loaded the referenced Assembly from file
                if (loadedAssembly is not null)
                {
                    ValidateAssemblyVersions(logger, referencedAssemblyName, loadedAssembly.GetName());
                    continue;
                }
            }
            else
            {
                logger.LogWarning("Referenced Assembly {AssemblyFullName} is not at {Path}", referencedAssemblyName.FullName, dllPath);
            }

            try
            {
                // we could not load the referenced Assembly from a specific path (or the file did not exist) so we
                // now have to try Assembly.Load which probes different paths
                loadedAssembly = Assembly.Load(referencedAssemblyName.FullName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to load Assembly {AssemblyFullName}", referencedAssemblyName.FullName);
                loadedAssembly = null;
            }

            // .NET Framework found the Assembly and loaded it successfully
            if (loadedAssembly is not null)
            {
                ValidateAssemblyVersions(logger, referencedAssemblyName, loadedAssembly.GetName());
                continue;
            }

            // nothing works so just throw
            var exception = new Exception($"Missing Assembly {referencedAssemblyName.FullName}");
            logger.LogError(exception, null);
            throw exception;
        }
    }

    private static void ValidateAssemblyVersions(ILogger logger, AssemblyName expected, AssemblyName actual)
    {
        var expectedVersion = expected.Version;
        var actualVersion = actual.Version;

        if (expectedVersion.Major != actualVersion.Major)
            LogThrow(logger, $"Version mismatch for Assembly {expected.Name}! expected: {expectedVersion} actual: {actualVersion}");
    }

    private static void LogThrow(ILogger logger, string msg)
    {
        var e = new Exception(msg);
        logger.LogError(e, null);
        throw e;
    }
}

