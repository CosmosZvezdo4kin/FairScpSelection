using System;
using CommandSystem;
using LabApi.Features.Permissions;
using PlayerRoles.RoleAssign;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ResetScpOverflow : ICommand, IUsageProvider
{
    public string Command => "resetscpoverflow";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Completely resets the \"ScpOverflow\" compensation system bonus";
    public string[] Usage => Array.Empty<string>();
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

        RoleAssigner.ScpsOverflowing = false;
        RoleAssigner.ScpOverflowMaxHsMultiplier = 1f;

        response = "You have reset the \"ScpOverflow\" compensation system bonus";
        return true;
    }
}