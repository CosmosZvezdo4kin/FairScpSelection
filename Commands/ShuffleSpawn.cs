using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ShuffleSpawn : ICommand, IUsageProvider
{
    public string Command => "shufflespawn";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Allows enabling or disabling sorting by players' SCP tickets during role assignment";
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
        
        Core.Singleton.ShouldOrderListByTickets = !Core.Singleton.ShouldOrderListByTickets;

        response = Core.Singleton.ShouldOrderListByTickets ? "You have enabled sorting by tickets" : "You have disabled ticket-based sorting (it’s now completely random)";
        return true;
    }
}