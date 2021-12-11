using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sharprompt;

namespace SmartHomeApi.WebApi.CLI
{
    public class CLIMainMenu : ICommand
    {
        private const string CreateAppsettings = "Create appsettings.json";
        private const string ExitCLIAndRunSmartHomeApi = "Exit CLI and run SmartHomeApi";
        private const string GenerateInstallUninstallServiceScripts = "Generate install/uninstall service scripts";
        private const string Menu = "Menu";
        private const string MenuItem = "MenuItem";
        private const string AreYouSure = "Are you sure?";
        private const string UnknownCommand = "Unknown command";
        private const string Exit = "Exit";

        public async Task<int> Execute(CLIContext cliCtx)
        {
            while (true)
            {
                Console.WriteLine();

                string selectedMenuItem;

                if (cliCtx.Options.ContainsKey(MenuItem))
                {
                    selectedMenuItem = cliCtx.Options[MenuItem].ToString();
                    cliCtx.Options.Remove(MenuItem);
                }
                else
                {
                    var menuItems = new List<string>();
                    menuItems.Add(CreateAppsettings);
                    menuItems.Add(GenerateInstallUninstallServiceScripts);

                    //In Linux (at least in ubuntu) it's impossible to stop app with ctrl+c when it's run from this menu 
                    //and when you just close terminal it does not release port.
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        menuItems.Add(ExitCLIAndRunSmartHomeApi);
                    }

                    menuItems.Add(Exit);

                    selectedMenuItem = Prompt.Select(Menu, menuItems);
                }

                switch (selectedMenuItem)
                {
                    case CreateAppsettings:
                        await new CreateAppsettingsCmd().Execute(cliCtx);
                        break;
                    case ExitCLIAndRunSmartHomeApi:
                        if (Prompt.Confirm(AreYouSure))
                            return 2;

                        break;
                    case GenerateInstallUninstallServiceScripts:
                        await new CreateServiceScriptsCmd().Execute(cliCtx);
                        break;
                    case Exit:
                        if (Prompt.Confirm(AreYouSure))
                            return 0;

                        break;
                    default:
                        Console.WriteLine(UnknownCommand);
                        break;
                }
            }
        }
    }
}