using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class AllowScpOverflow : ICommand, IUsageProvider
{
    public string Command => "allowscpoverflow";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Allows enabling or disabling \"ScpOverflow\" compensation system. When it's disabled (false) SCPs will get bonus max HS and max HP if target SCP number exceeds the role limit";
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
        
        Core.Singleton.AllowScpOverflow = !Core.Singleton.AllowScpOverflow;

        response = Core.Singleton.AllowScpOverflow ? "You have disabled \"ScpOverflow\" compensation system (SCPs wont get any bonus max HS and max HP on start)" : "You have enabled \"ScpOverflow\" compensation system (SCPs can gain a bonus max HS and max HP at start)";
        return true;
    }
}