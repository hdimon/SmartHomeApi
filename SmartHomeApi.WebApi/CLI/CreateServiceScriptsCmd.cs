using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sharprompt;

namespace SmartHomeApi.WebApi.CLI
{
    public class CreateServiceScriptsCmd : ICommand
    {
        private const string ScriptsDirectory = "Scripts";
        private const string ScriptFilePrefix = "SmartHomeApi.WebApi.ServiceScripts";

        public async Task<int> Execute(CLIContext cliCtx)
        {
            var scriptsPath = Path.Combine(Directory.GetCurrentDirectory(), ScriptsDirectory);

            Console.WriteLine($"Scripts for installing SmartHomeApi as service will be created in {scriptsPath}.");

            if (!Prompt.Confirm("Do you want to proceed?"))
                return 0;

            Directory.CreateDirectory(scriptsPath);

            var assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
            {
                ConsoleWriteLineError("Can't get info about assembly.");
                return 1;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return await GenerateScripts(assembly, scriptsPath, "Linux");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await GenerateScripts(assembly, scriptsPath, "Windows");

            Console.WriteLine("Scripts for your OS is not found.");

            return 1;
        }

        private Task<int> GenerateScripts(Assembly assembly, string scriptsPath, string osPlatform)
        {
            string[] names = assembly.GetManifestResourceNames();

            var filePrefix = $"{ScriptFilePrefix}.{osPlatform}.";
            var fileNames = names.Where(f => f.StartsWith(filePrefix)).ToList();

            if (!fileNames.Any()) return Task.FromResult(1);

            foreach (var fullName in fileNames)
            {
                var name = fullName.Replace(filePrefix, string.Empty);
                string file;

                using (var stream = assembly.GetManifestResourceStream(fullName))
                {
                    if (stream == null)
                    {
                        ConsoleWriteLineError("Can't get embedded resources.");
                        return Task.FromResult(1);
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        file = reader.ReadToEnd();
                    }
                }

                if (string.IsNullOrWhiteSpace(file))
                {
                    ConsoleWriteLineError($"{name} is empty.");
                    return Task.FromResult(1);
                }

                File.WriteAllText(Path.Combine(scriptsPath, name), file, Encoding.ASCII);
            }

            return Task.FromResult(0);
        }

        private void ConsoleWriteLineError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}