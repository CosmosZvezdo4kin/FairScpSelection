using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class TargetRolesNumber : ICommand, IUsageProvider
{
    public string Command => "targetrolesnumber";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Allows you to change the number of roles required for a spawn (default is -1)";
    public string[] Usage => new string[] { "(Count)" };
    private string RequiredPermission => "fss." + Command;
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasAnyPermission(RequiredPermission))
        {
            response = $"Not enough permissions ({RequiredPermission})";
            return false;
        }
            
        if (Core.Singleton == null)
        {
            response = "Plugin is disabled (FairScpSelection)";
            return false;
        }
        
        if (arguments.Count < 1)
        {
            response = $"Current TargetRolesNumber value is <b>{Core.Singleton.TargetRolesNumber}</b>";
            return true;
        }

        if (!int.TryParse(arguments.At(0), out var targetRolesNumber) || targetRolesNumber < -1)
        {
            response = "Invalid argument (must be an integer greater than or equal to -1)";
            return false;
        }
        
        Core.Singleton.TargetRolesNumber = targetRolesNumber;

        response = $"The new value of TargetRolesNumber is set to <b>{targetRolesNumber}</b>";
        return true;
    }
}