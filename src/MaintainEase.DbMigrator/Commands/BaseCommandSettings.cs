using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaintainEase.DbMigrator.Commands
{
    /// <summary>
    /// Base settings class for all commands
    /// </summary>
    public class BaseCommandSettings : CommandSettings
    {
        [CommandOption("-e|--environment")]
        [Description("Target environment (e.g., Development, Staging, Production)")]
        public string? Environment { get; set; }

        [CommandOption("-t|--tenant")]
        [Description("Specific tenant to target")]
        public string? Tenant { get; set; }

        [CommandOption("-v|--verbose")]
        [Description("Show verbose output")]
        public bool Verbose { get; set; }

        [CommandOption("--no-prompt")]
        [Description("Skip confirmation prompts")]
        public bool NoPrompt { get; set; }
    }
}
