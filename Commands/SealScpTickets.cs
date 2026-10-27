using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SealScpTickets : ICommand, IUsageProvider
{
    public string Command => "sealscptickets";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Allows enabling or disabling the ability to modify tickets via internal systems (excluding commands)";
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
        
        Core.Singleton.ShouldModifyTickets = !Core.Singleton.ShouldModifyTickets;

        response = Core.Singleton.ShouldModifyTickets ? "You have enabled the option to modify tickets" : "You have disabled the ability to modify tickets";
        return true;
    }
}