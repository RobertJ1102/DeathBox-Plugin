using Rocket.API;
using Rocket.Core.Logging;
using System.Collections.Generic;

namespace DeathBox.Commands
{
    public class DebugCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Console;
        public string Name => "debug";
        public string Help => string.Empty;
        public string Syntax => "/debug";
        public List<string> Aliases => new List<string> { "dbdebug" };
        public List<string> Permissions => new List<string> { "deathbox.debug" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Main.DebugMode = !Main.DebugMode;
            Logger.Log($"[DEBUG MODE] Debug state changed to {Main.DebugMode}.");
        }
    }
}
