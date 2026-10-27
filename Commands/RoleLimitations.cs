using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using PlayerRoles;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class RoleLimitations : ICommand, IUsageProvider
{
    public string Command => "rolelimitations";
    public string[] Aliases => new string[] { "rolelimits" };
    public string Description => "Allows you to view (without arguments) or modify the limits on the number of times specific roles can appear in a round";
    public string[] Usage => new string[] { "(RoleType or RoleType1.RoleType2...)", "(Count)" };
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
            var limitationList = Core.Singleton.RoleLimitations.Select(x => $"<b>{x.Key}</b> - {x.Value}");
            
            response = Core.Singleton.RoleLimitations.Any() ? $"Current values for role limits:\n{string.Join("\n", limitationList)}" : "The current limit values are empty :(";
            return true;
        }

        if (arguments.Count < 2)
        {
            response = "Not enough arguments:\n" + this.DisplayCommandUsage();
            return false;
        }
        
        var stringRoleTypes = arguments.At(0).Split('.');
        var roleTypes = new HashSet<RoleTypeId>();

        foreach (var stringRoleType in stringRoleTypes)
        {
            if (!Enum.TryParse(stringRoleType, true, out RoleTypeId roleType))
            {
                response = $"There is no such role - {stringRoleType}:\n" + this.DisplayCommandUsage();
                return false;
            }
            
            roleTypes.Add(roleType);
        }

        if (!int.TryParse(arguments.At(1), out var limitValue) || limitValue < 0)
        {
            response = "Invalid 2nd argument (must be an integer greater than or equal to 0)";
            return false;
        }

        foreach (var roleType in roleTypes)
            Core.Singleton.RoleLimitations[roleType] = limitValue;

        response = $"New limit value has been set for the roles {string.Join(".", roleTypes)} <b>{limitValue}</b>";
        return true;
    }
}