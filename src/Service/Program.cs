using Serilog;
using System;
using System.Reflection;
using System.Security;
using Topshelf;

namespace OneIdentity.ARSGJitAccess.Service
{
    public class Program
    {
        
        public static readonly string AppName = "ARSGJitAccess";
        public static readonly string AppDisplayName = "Active Roles/Safeguard Just-in-time Access";
        public static readonly string AppDescription = "Listens for Safeguard access requests and call Active Roles to set permission granting attribute";

        public static void Main()
        {
            bool isTest = false;

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.EventLog(AppName, manageEventSource: true)
                    .CreateLogger();
            }
            catch(SecurityException)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
                Log.Warning("Unable to access Windows Event Log.  Logging console only");
            }

            var rc = HostFactory.Run(x => 
            {
                Type t = typeof(Program);
                Assembly assembly = t.Assembly;

                x.Service<Service>(hostSettings => new Service(isTest));
                x.RunAsLocalSystem();
                x.UseSerilog();
                x.SetServiceName(assembly.GetName().Name);
                x.SetDisplayName(assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title);
                x.SetDescription(assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description);
                x.AddCommandLineDefinition("test", v => { isTest = true; } );
                x.AddCommandLineDefinition("config", v =>
                {
                    isTest = true;
                    Config.ConfigureAppSettings();
                });
                x.AddCommandLineDefinition("installAndConfigureService", v =>
                {
                    isTest = true;
                    Config.ConfigureAppSettings();
                    Config.InstallService();
                });
                x.AddCommandLineDefinition("uninstallService", v=>Config.UninstallService());
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());  
            Environment.ExitCode = exitCode;
        }
    }
}
