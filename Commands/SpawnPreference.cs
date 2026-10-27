using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using InventorySystem.Items.ToggleableLights.Flashlight;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.RoleAssign;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SpawnPreference : ICommand, IUsageProvider
{
    public string Command => "spawnpreference";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Allows you to view (with only one argument) or modify the specific role spawn preference for player";
    public string[] Usage => new string[] { "PlayerId", "(RoleType)", "(Count)" };
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
            response = "Not enough arguments:\n" + this.DisplayCommandUsage();
            return false;
        }
        
        var stringPlayerId = arguments.At(0);
        
        if (!int.TryParse(stringPlayerId, out var playerId))
        {
            response = $"Valid PlayerId was not provided - {stringPlayerId}\n" + this.DisplayCommandUsage();
            return false;
        }

        if (!Player.TryGet(playerId, out var player) || player.IsDestroyed || !player.IsReady || player.IsHost || (!player.IsPlayer && !Core.Singleton.Config.Dummy))
        {
            response = $"Player with this PlayerId was not found - {stringPlayerId}\n" + this.DisplayCommandUsage();
            return false;
        }
        
        if (!ScpSpawnPreferences.Preferences.ContainsKey(player.Connection.connectionId) && player.IsDummy)
            ScpSpawnPreferences.Preferences[player.Connection.connectionId] = new ScpSpawnPreferences.SpawnPreferences(false);

        if (!ScpSpawnPreferences.Preferences.TryGetValue(player.Connection.connectionId, out var spawnPreferences))
        {
            response = "This player has no spawn preferences";
            return false;
        }

        if (arguments.Count < 2)
        {
            var preferencesList = spawnPreferences.Preferences.Select(p => $"{p.Key}: {p.Value + 5}");
            
            response = spawnPreferences.Preferences.Any() ? $"Current values for role spawn preferences:\n{string.Join("\n", preferencesList)}" : "The current role spawn preference values are empty :(";
            return true;
        }
        
        if (!Enum.TryParse(arguments.At(1), true, out RoleTypeId roleType))
        {
            response = $"There is no such role - {arguments.At(1)}:\n" + this.DisplayCommandUsage();
            return false;
        }
            
        if (!int.TryParse(arguments.At(2), out var preferenceValue) || preferenceValue < 0 || preferenceValue > 10)
        {
            response = "Invalid 2nd argument (must be an integer from 0 to 10 inclusive)";
            return false;
        }

        spawnPreferences.Preferences[roleType] = preferenceValue - 5;
        
        response = $"Set {player.Nickname} ({player.PlayerId}) spawn preference for {roleType} is <b>{preferenceValue}</b>";
        return true;
    }
}