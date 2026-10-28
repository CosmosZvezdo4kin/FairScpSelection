using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using PlayerRoles;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class PredictSpawnRoles : ICommand, IUsageProvider
{
    public string Command => "predictspawnroles";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "It literally does what it says";
    public string[] Usage => new string[] { "Count" };
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
        
        if (Core.Singleton.TicketLoader == null || !Core.Singleton.TicketLoader.IsReady)
        {
            response = "TicketLoader is null or not ready";
            return false;
        }
        
        if (arguments.Count < 1)
        {
            response = "Not enough arguments\n" + this.DisplayCommandUsage();
            return false;
        }

        if (!int.TryParse(arguments.At(0), out var targetRolesNumber) || targetRolesNumber <= 0)
        {
            response = "Invalid argument (must be an integer greater than 0)";
            return false;
        }
        
        var ticketsEntries = Core.Singleton.TicketLoader.TicketsEntries;

        var predictedSpawnRoles = Core.Singleton.PredictSpawnRoles(ticketsEntries, targetRolesNumber, out var spawnReason);

        var predictedSpawnResult = string.Empty;
        
        foreach (var kvp in predictedSpawnRoles.Where(x => x.Value.IsAlive() && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost && (x.Key.IsPlayer || Core.Singleton.Config.DummyMode)))
        {
            var player = kvp.Key;
            var roleType = kvp.Value;

            var tickets = 0;

            if (ticketsEntries.TryGetValue(player.UserId, out var ticketEntry))
                tickets = ticketEntry.Tickets;
            
            predictedSpawnResult += $"- [{player.PlayerId}] {roleType} / {tickets} / {spawnReason[player]}\n";
        }
        
        response = !string.IsNullOrWhiteSpace(predictedSpawnResult) ? $"Predicted spawn result:\n{predictedSpawnResult}" : "No results :(";
        return true;
    }
}