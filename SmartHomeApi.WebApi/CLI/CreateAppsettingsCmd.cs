using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using Newtonsoft.Json;
using Sharprompt;

namespace SmartHomeApi.WebApi.CLI
{
    public class CreateAppsettingsCmd : ICommand
    {
        private const int DefaultPort = 5100;
        private const int MaxPort = 65535;
        private const string AppsettingsTemplateFileName = "SmartHomeApi.WebApi.install.appsettings.json";
        private const string AppsettingsFileName = "appsettings.json";
        private const string Yes = "Yes";
        private const string No = "No";
        private const string SmartHomeApiDataDirectoryName = "SmartHomeApiData";
        private const string IWantToUseAnotherDirectory = "I want to use another directory";
        private const string CancelInstallation = "Cancel installation";
        private const string IWantToUseAnotherPort = "I want to use another port";

        public Task<int> Execute(CLIContext cliCtx)
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), AppsettingsFileName)))
            {
                var selected = Prompt.Select("appsettings.json already exists. Do you want to recreate it?", new[] { Yes, No });

                if (selected == No) return Task.FromResult(0);
            }

            var parameters = new AppsettingsParameters();

            var dataDirectoryResult = CreateDataDirectory(parameters);

            if (dataDirectoryResult == OperationResult.Exit) return Task.FromResult(0);

            var portResult = ChoosePort(parameters);

            if (portResult == OperationResult.Exit) return Task.FromResult(0);

            var localeResult = ChooseLocale(parameters);

            if (localeResult == OperationResult.Exit) return Task.FromResult(0);

            CreateAppsettingsFile(parameters);

            return Task.FromResult(0);
        }

        private void ConsoleWriteLineError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private OperationResult CreateDataDirectory(AppsettingsParameters parameters)
        {
            var currentDirectoryPath = Directory.GetCurrentDirectory();

            Console.WriteLine("SmartHomeApi requires data directory for plugins, temporary files, configuration files and so on. " +
                              $"Current directory is {currentDirectoryPath}.");

            var dataDirectoryPath = GetDefaultDataDirectoryPath();
            dataDirectoryPath = ChooseDataDirectoryPath(dataDirectoryPath);

            if (dataDirectoryPath == null) return OperationResult.Exit;

            parameters.DataDirectoryPath = dataDirectoryPath;

            if (!Directory.Exists(parameters.DataDirectoryPath))
            {
                try
                {
                    Directory.CreateDirectory(parameters.DataDirectoryPath);
                }
                catch (Exception e)
                {
                    ConsoleWriteLineError(e.Message);
                    Console.WriteLine(e);

                    return OperationResult.Exit;
                }
            }

            return OperationResult.Continue;
        }

        private string GetDefaultDataDirectoryPath()
        {
            //Yes, it's ok for now. No strategies, abstract fabrics and so on.
            //Just if..else till OS specific stuff starts to grow.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine("/var", "opt", SmartHomeApiDataDirectoryName);
            }

            var currentDirectoryPath = Directory.GetCurrentDirectory();
            var dataDirectoryPath = Path.Combine(currentDirectoryPath, SmartHomeApiDataDirectoryName);

            return dataDirectoryPath;
        }

        private string ChooseDataDirectoryPath(string dataDirectoryPath)
        {
            if (Directory.Exists(dataDirectoryPath))
            {
                Console.WriteLine($"Directory {dataDirectoryPath} already exists. Would you like to use it as data directory?");

                return ShowDataDirectoryMenuPrompt(dataDirectoryPath);
            }

            Console.WriteLine($"Would you like to create {dataDirectoryPath} and use it as data directory?");

            return ShowDataDirectoryMenuPrompt(dataDirectoryPath);
        }

        private string ShowDataDirectoryMenuPrompt(string dataDirectoryPath)
        {
            var selected = Prompt.Select("", new[] { Yes, IWantToUseAnotherDirectory, CancelInstallation });

            if (selected == CancelInstallation) return null;

            if (selected == Yes)
            {
                return dataDirectoryPath;
            }

            if (selected == IWantToUseAnotherDirectory)
            {
                var path = Prompt.Input<string>("Edit path to data directory", dataDirectoryPath);

                return ChooseDataDirectoryPath(path);
            }

            return null;
        }

        private OperationResult ChoosePort(AppsettingsParameters parameters)
        {
            bool portIsAvailable = false;
            int iteration = 0;
            var checkedPorts = new List<int>();

            int port = -1;

            while (!portIsAvailable && iteration < MaxPort)
            {
                port = GetPortCandidate(checkedPorts);

                portIsAvailable = PortIsAvailable(port);
                iteration++;
            }

            Console.WriteLine($"SmartHomeApi server requires port. Would you like to use port {port}?");

            var selected = Prompt.Select("", new[] { Yes, IWantToUseAnotherPort, CancelInstallation });

            if (selected == CancelInstallation) return OperationResult.Exit;

            if (selected == Yes)
            {
                parameters.Port = port;

                return OperationResult.Continue;
            }

            if (selected == IWantToUseAnotherPort)
            {
                bool isValid;
                var isAvail = false;
                int p = -1;

                do
                {
                    if (p != -1)
                    {
                        var contOpt = Prompt.Select("Do you want to use another port?", new[] { Yes, CancelInstallation });

                        if (contOpt == CancelInstallation) return OperationResult.Exit;
                    }

                    p = Prompt.Input<int>("Enter port", port);

                    isValid = p is > 0 and <= MaxPort;

                    if (!isValid)
                    {
                        ConsoleWriteLineError($"Port must be in range between 1 and {MaxPort}.");
                        continue;
                    }

                    isAvail = PortIsAvailable(p);

                    if (!isAvail)
                    {
                        ConsoleWriteLineError("Port is used by another application.");
                    }
                } while (!isValid || !isAvail);

                Console.WriteLine($"Port {p} is available and will be used.");

                parameters.Port = p;

                return OperationResult.Continue;
            }

            parameters.Port = port;

            return OperationResult.Continue;
        }

        private int GetPortCandidate(List<int> checkedPorts)
        {
            int iteration = 0;
            Random r = new Random();
            int port = DefaultPort;

            while (checkedPorts.Contains(port) && iteration < MaxPort)
            {
                port = r.Next(1, MaxPort);

                iteration++;
            }

            checkedPorts.Add(port);

            return port;
        }

        private bool PortIsAvailable(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        private OperationResult ChooseLocale(AppsettingsParameters parameters)
        {
            var cultures = CultureInfoHelper.CultureNames;
            var current = Thread.CurrentThread.CurrentCulture.Name;

            var locale = Prompt.Select("Please select SmartHomeApi locale", cultures, 15, current);

            parameters.Locale = locale;

            return OperationResult.Continue;
        }

        private void CreateAppsettingsFile(AppsettingsParameters parameters)
        {
            var assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
            {
                ConsoleWriteLineError("Can't get info about assembly.");
                return;
            }

            string[] names = assembly.GetManifestResourceNames();

            if (!names.Contains(AppsettingsTemplateFileName))
            {
                ConsoleWriteLineError("Appsettings template is not found.");
                return;
            }

            string file;

            using (var stream = assembly.GetManifestResourceStream(AppsettingsTemplateFileName))
            {
                if (stream == null)
                {
                    ConsoleWriteLineError("Can't get embedded resources.");
                    return;
                }

                using (var reader = new StreamReader(stream))
                {
                    file = reader.ReadToEnd();
                }
            }

            if (string.IsNullOrWhiteSpace(file))
            {
                ConsoleWriteLineError("Appsettings template is empty.");
                return;
            }

            var logsPath = Path.Combine(parameters.DataDirectoryPath, "Logs", "SmartHomeApi-.log");

            file = file.Replace("API_CULTURE", parameters.Locale);
            file = file.Replace("\"DATA_DIRECTORY_PATH\"", JsonConvert.SerializeObject(parameters.DataDirectoryPath));
            file = file.Replace("\"DATA_DIRECTORY_LOGS_PATH\"", JsonConvert.SerializeObject(logsPath));
            file = file.Replace("API_PORT", parameters.Port.ToString());

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), AppsettingsFileName), file, Encoding.UTF8);
        }

        private enum OperationResult
        {
            Continue,
            Exit
        }

        private class AppsettingsParameters
        {
            public string DataDirectoryPath { get; set; }
            public int Port { get; set; }
            public string Locale { get; set; }
        }
    }
}