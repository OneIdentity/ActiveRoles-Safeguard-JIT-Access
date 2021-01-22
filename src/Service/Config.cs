using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Newtonsoft.Json.Linq;
using OneIdentity.ARSGJitAccess.Common;
using OneIdentity.SafeguardDotNet;
using Serilog;

namespace OneIdentity.ARSGJitAccess.Service
{
    public class Config
    {
        public static string ActiveRolesUsername
        {
            get
            {
                return EmptyAsNull("ActiveRolesUsername");
            }
            private set
            {
                UpdateAppSetting("ActiveRolesUsername",value);
            }
        }

        public static string ActiveRolesPassword
        {
            get
            {
                return EmptyAsNull("ActiveRolesPassword");
            }
            private set
            {
                UpdateAppSetting("ActiveRolesPassword", value);
            }
        }

        public static string ARSGJitAccessAttribute
        {
            get
            {
                return EmptyAsNull("ARSGJitAccessAttribute");
            }
            private set
            {
                UpdateAppSetting("ARSGJitAccessAttribute", value);
            }
        }

        public static string SafeguardAppliance
        {
            get
            {
                return EmptyAsNull("SafeguardAppliance");
            }
            private set
            {
                UpdateAppSetting("SafeguardAppliance", value);
            }
        }

        public static string SafeguardCertificateThumbprint
        {
            get
            {
                return EmptyAsNull("SafeguardCertificateThumbprint");
            }
            private set
            {
                UpdateAppSetting("SafeguardCertificateThumbprint", value);
            }
        }

        public static string SafeguardCertificateFile
        {
            get
            {
                return EmptyAsNull("SafeguardCertificateFile");
            }
            private set
            {
                UpdateAppSetting("SafeguardCertificateFile", value);
            }
        }

        public static SecureString SafeguardCertificatePassword
        {
            get
            {
                return AsSecureStringEmptyAsNull("SafeguardCertificatePassword");
            }
            private set
            {
                UpdateAppSetting("SafeguardCertificatePassword", value.ToInsecureString());
            }
        }

        public static string SafeguardUsername
        {
            get
            {
                return EmptyAsNull("SafeguardUsername");
            }
            private set
            {
                UpdateAppSetting("SafeguardUsername", value);
            }
        }

        internal static void SetConfigFile(string v)
        {
            _configPath = v.Replace(".config","");

            File.Create(_configPath).Close();
            _configFile = ConfigurationManager.OpenExeConfiguration(_configPath);
            File.Delete(_configPath);
        }

        public static SecureString SafeguardPassword
        {
            get
            {
                return AsSecureStringEmptyAsNull("SafeguardPassword");
            }
            private set
            {
                UpdateAppSetting("SafeguardPassword", value.ToInsecureString());
            }
        }

        static string EmptyAsNull(string setting)
        {
            var configFile = _configFile ?? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var value = configFile.AppSettings.Settings[setting]?.Value;

            if (value == null || value.Length == 0)
            {
                return null;
            }
            return value;
        }

        static SecureString AsSecureStringEmptyAsNull(string setting)
        {
            var configFile = _configFile ?? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var value = configFile.AppSettings.Settings[setting]?.Value;

            if (value == null || value.Length == 0)
            {
                return null;
            }

            SecureString secureString = ConvertToSecureString(value);

            return secureString;
        }

        private static SecureString ConvertToSecureString(string s)
        {
            SecureString secureString = new SecureString();
            s.ToCharArray().ToList().ForEach(secureString.AppendChar);
            secureString.MakeReadOnly();
            return secureString;
        }

        private static string _configPath = null;
        private static Configuration _configFile = null; 

        public static void ConfigureFromFile()
        {

            Console.WriteLine("--------------------------------");
            Console.WriteLine("ARSGJitAccess File Configuration");
            Console.WriteLine("--------------------------------");

            string dir = null;
            while (!Directory.Exists(dir))
            {
                dir = GetSettingInput("Enter Configuration Directory Path. Empty for default");
                if (string.IsNullOrEmpty(dir))
                {
                    Console.WriteLine("Using default");
                    _configPath = null;
                    return;
                }
            }

            Console.WriteLine($"Directory {dir} exists.");

            var fileName = GetSettingInput("Enter Configuration filename. Empty for default (ARSGJIT.config)");
            if (string.IsNullOrEmpty(fileName))
                fileName = "ARSGJIT";
            else
                fileName = fileName.Split('.')[0];

            try
            {
                _configPath = Path.Combine(dir, fileName);

                //This is a little acrobatic, but hear me out. 
                //Using System.Configuration makes config read / write consistent regardless of whether it's the app.config or 
                //a user's config file. 
                //For that to work, ConfigurationManager.OpenExeConfiguration() needs an exe to open, or create, a config file.
                //so we temporarily create an exe with the path and filename provided by the user, then open the configuration,
                //then delete the exe. leaving us with file <filename>.exe.config. 
                //Even when provided a file with .config extension, OpenExeConfiguration creates a new file with .config
                //i.e, if provided MyArsgConfig.config, it would create and use MyArsgConfig.config.config. 
                File.Create(_configPath + ".exe").Close();
                _configFile = ConfigurationManager.OpenExeConfiguration(_configPath + ".exe");
                File.Delete(_configPath + ".exe");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error: failed to create file {_configPath}. Message: {ex.Message}");
                System.Environment.Exit(1);
            }

            Console.WriteLine($"File {_configPath}.exe.config successfully created.");

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("ARSGJitAccess File Configuration Complete");
            Console.WriteLine("-----------------------------------------");
        }

        public static void ConfigureAppSettings()
        {
            Console.WriteLine("---------------------------");
            Console.WriteLine("ARSGJitAccess Configuration");
            Console.WriteLine("---------------------------");

            int serviceAuthTypeSelection = GetServiceAuthType();
            ConfigureServiceAuthType(serviceAuthTypeSelection);

            ARSGJitAccessAttribute = GetSettingInput("ARS Access Attribute", ARSGJitAccessAttribute);

            SafeguardAppliance = GetAndValidateApplianceAddress();

            int safeguardAuthTypeSelection = GetSafeguardAuthType();
            ConfigureSafeguardAuthType(safeguardAuthTypeSelection);

            Console.WriteLine("------------------------------------");
            Console.WriteLine("ARSGJitAccess Configuration Complete");
            Console.WriteLine("------------------------------------");
        }

        private static void ConfigureSafeguardAuthType(int SafeguardAuthType)
        {
            switch (SafeguardAuthType)
            {
                //no input; skip
                case 0:
                    break;

                case 1:
                    SafeguardCertificateThumbprint = GetAndValidateSafeguardThumbprint();

                    ClearAppSetting("SafeguardCertificateFile");
                    ClearAppSetting("SafeguardCertificatePassword");
                    ClearAppSetting("SafeguardUsername");
                    ClearAppSetting("SafeguardPassword");

                    break;

                case 2:
                    SafeguardCertificateFile = GetAndValidateSafeguardCertificate();

                    var safeguardCertificatePassword = SafeguardCertificatePassword == null ?
                        GetSettingInput("Safeguard Certificate Password") :
                        GetSettingInput("Safeguard Certificate Password", new string('*', SafeguardCertificatePassword.Length));

                    SafeguardCertificatePassword = ConvertToSecureString(safeguardCertificatePassword);

                    ClearAppSetting("SafeguardCertificateThumbprint");
                    ClearAppSetting("SafeguardUsername");
                    ClearAppSetting("SafeguardPassword");

                    break;

                case 3:
                    var safeguardUsername = GetSettingInput("Safeguard UserName", SafeguardUsername);
                    var safeguardPassword = SafeguardPassword == null ?
                        GetSettingInput("Safeguard User Password") :
                        GetSettingInput("Safeguard User Password", new string('*', SafeguardPassword.Length));

                    SafeguardUsername = safeguardUsername;
                    SafeguardPassword = ConvertToSecureString(safeguardPassword);

                    ClearAppSetting("SafeguardCertificateThumbprint");
                    ClearAppSetting("SafeguardCertificateFile");
                    ClearAppSetting("SafeguardCertificatePassword");

                    break;
            }
        }

        private static void ConfigureServiceAuthType(int ServiceAuthType)
        {
            switch (ServiceAuthType)
            {
                //no input; skip
                case 0:
                    break;
                //Log on as Service option
                case 1:
                    ClearAppSetting("ActiveRolesUsername");
                    ClearAppSetting("ActiveRolesPassword");

                    break;
                //Username / Password option
                case 2:
                    ActiveRolesUsername = GetSettingInput("ARS Username", ActiveRolesUsername);

                    ActiveRolesPassword = string.IsNullOrEmpty(ActiveRolesPassword) ?
                        GetSettingInput("Initial ARS Password") :
                        GetSettingInput("Initial ARS Password", new string('*', ActiveRolesPassword.Length));
                    break;
            }
        }

        private static string GetAndValidateSafeguardCertificate()
        {
            var isCert = false;

            var certPath = "";
            while(!isCert)
            {
                certPath = GetSettingInput("Safeguard User Certificate File Path", SafeguardCertificateFile);

                if (String.IsNullOrEmpty(certPath))
                    return certPath;
              
                try
                {
                    var certificate = new X509Certificate();
                    certificate.Import(certPath);
                    isCert = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\tError verifying certificate: {ex.Message}");
                }
            }

            Console.WriteLine("\tCertificate validation succeeded.");

            return certPath;
        }

        private static string GetAndValidateSafeguardThumbprint()
        {
            var emptyThumbprintString = -1;
            var matchingCertCount = 0;

            string safeguardCertThumbprint = "";
            while(matchingCertCount <= 0)
            {
                safeguardCertThumbprint = GetSettingInput("Safeguard User Certificate Thumbprint",
                    SafeguardCertificateThumbprint);

                matchingCertCount = ValidateSafeguardCertThumbprint(safeguardCertThumbprint);

                if(matchingCertCount == emptyThumbprintString)
                {
                    var resp = GetSettingInput(
                       $"\tProvided certificate thumbprint was empty. Continue?",
                       "y/n");

                    if (String.CompareOrdinal(resp, "y") == 0)
                        break;
                }
                else if (matchingCertCount == 0)
                {
                    var resp = GetSettingInput(
                        $"\tNo certificate with thumbprint {safeguardCertThumbprint} found in current user or local machine certificate store. Continue?",
                        "y/n");

                    if (String.CompareOrdinal(resp, "y") == 0)
                        break;
                }
            }

            if(!String.IsNullOrEmpty(safeguardCertThumbprint) && matchingCertCount > 0)
                Console.WriteLine($"\tFound certificate with thumbprint {safeguardCertThumbprint}.");
            
            return safeguardCertThumbprint;
        }

        private static int ValidateSafeguardCertThumbprint(string safeguardCertThumbprint)
        {
            var emptyThumbprintString = -1;

            X509Certificate2Collection matchingCerts = new X509Certificate2Collection();

            if (safeguardCertThumbprint == "")
                return emptyThumbprintString;

            matchingCerts = GetMatchingCerts(safeguardCertThumbprint);

            return matchingCerts.Count;
        }

        private static X509Certificate2Collection GetMatchingCerts(string thumbprint, bool searchLocalMachine = true)
        {
            X509Store currentUserCertStore = new X509Store(StoreLocation.CurrentUser);
            currentUserCertStore.Open(OpenFlags.ReadOnly);

            var matchingCerts = currentUserCertStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

            if(searchLocalMachine)
            {
                X509Store localMachineCertStore = new X509Store(StoreLocation.LocalMachine);
                localMachineCertStore.Open(OpenFlags.ReadOnly);

                var localMachineMatchingCerts = localMachineCertStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint,
                    false);

                matchingCerts.AddRange(localMachineMatchingCerts);
            }

            return matchingCerts;
        }

        private static string GetAndValidateApplianceAddress()
        {
            string applianceAddress = null;
            JObject response = null;
            var isValid = false;
           while (response == null && !isValid)
            {
                applianceAddress = GetSettingInput("Safeguard Appliance Address", SafeguardAppliance);
                
                if (string.IsNullOrEmpty(applianceAddress))
                    return applianceAddress;

                isValid = SafeguardRestApiClient.ValidateSafeguardApplianceAddress(applianceAddress, out response);
            }

            Console.WriteLine($"\tAppliance {response["Name"]} was found at {applianceAddress}");

            return applianceAddress;
        }

        private static int GetServiceAuthType()
        {
            var instruction = "Select Service Authentication Type";
            var options = new List<string>
            {
                "Run As Service",
                "Password in Config (DEBUG ONLY)"
            };
            var authType = GetAuthType(instruction, options);
            if (authType == 2)
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Warning: Username and Password stored in config in plain text");
                Console.ForegroundColor = prevColor;
            }   

            return authType;
        }

        private static int GetSafeguardAuthType()
        {
            var instruction = "Select Safeguard Authentication Type";
            var options = new List<string>
            {
                "Thumbprint",
                "Certificate File",
                "Password"
            };

            return GetAuthType(instruction, options);
        }

        private static int GetAuthType(string instruction, List<string> options)
        {
            var authType = 0;

            var instructionString = $"{instruction}: " +
                                    string.Join("; ", options.Select(x => $"({options.IndexOf(x) + 1}) {x}"));

            var authTypeStr = "";
            authTypeStr = GetSettingInput(instructionString);
            while (!string.IsNullOrEmpty(authTypeStr) && (!int.TryParse(authTypeStr, out authType) || authType < 1 || authType > 3))
                authTypeStr = GetSettingInput(instructionString);
            return authType;

        }

        private static string GetSettingInput(string instruction, string currentValue = null)
        {
            Console.Write(String.IsNullOrEmpty(currentValue) ? $"{instruction}: " : $"{instruction} ({currentValue}): ");

            return Console.ReadLine();
        }

        private static void ClearAppSetting(string key)
        {
            try
            {
                var configFile = _configFile ?? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                var settings = configFile.AppSettings.Settings;

                if (settings[key] == null)
                    return;
                else
                    settings.Remove(key);

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException ex)
            {
                Log.Fatal($"Error removing app settings: {ex.Message}");
            }
        }

        private static void UpdateAppSetting(string key, string value)
        {
            //If no value provided, retain existing setting.
            if (String.IsNullOrEmpty(value))
                return;

            try
            {
                var configFile = _configFile ?? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                var settings = configFile.AppSettings.Settings;

                if (settings[key] == null)
                    settings.Add(key, value);
                else
                    settings[key].Value = value;

                configFile.Save(ConfigurationSaveMode.Modified);
                
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException ex)
            {
                Log.Fatal($"Error updating app settings: {ex.Message}");
            }
        }

        public static void InstallService()
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine("ARSGJitAccess Service Install");
            Console.WriteLine("-----------------------------");

            var info = new ProcessStartInfo
            {
                Arguments = "install",
                FileName = "ARSGJitAccess.exe",
                UseShellExecute = false
            };

            using (var p = Process.Start(info))
                p.WaitForExit();

            Console.WriteLine("------------------------------------");
            Console.WriteLine("ARSGJitAccess Service Install Complete");
            Console.WriteLine("------------------------------------");

            Console.WriteLine("Press any key to continue...");
            Console.Read();

            Environment.Exit(0);
        }
        public static void UninstallService()
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine("ARSGJitAccess Service Uninstall");
            Console.WriteLine("-----------------------------");

            var info = new ProcessStartInfo
            {
                Arguments = "uninstall",
                FileName = "ARSGJitAccess.exe",
                UseShellExecute = false
            };

            using (var p = Process.Start(info))
                p.WaitForExit();

            Console.WriteLine("------------------------------------");
            Console.WriteLine("ARSGJitAccess Service Uninstall Complete");
            Console.WriteLine("------------------------------------");

            Console.WriteLine("Press any key to continue...");
            Console.Read();

            Environment.Exit(0);
        }
    }
}
