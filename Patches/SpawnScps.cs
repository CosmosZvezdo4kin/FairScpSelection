using System.Linq;
using HarmonyLib;
using LabApi.Features.Console;
using PlayerRoles;
using PlayerRoles.RoleAssign;

namespace FairScpSelection.Patches;

[HarmonyPatch(typeof(ScpSpawner), nameof(ScpSpawner.SpawnScps))]
public class SpawnScps
{
    public static bool Prefix(int targetScpNumber)
    {
        if (Core.Singleton == null)
            return true;

        if (Core.Singleton.TicketLoader == null || !Core.Singleton.TicketLoader.IsReady)
        {
            Logger.Error("TicketLoader is null or not ready");
            return true;
        }
        
        var ticketsEntries = Core.Singleton.TicketLoader.TicketsEntries;
        
        Logger.Debug("Predicting spawn roles ...", Core.Singleton.Config.Debug);
        
        var predictedSpawnRoles = Core.Singleton.PredictSpawnRoles(ticketsEntries, targetScpNumber, out var spawnReason);
        
        var spawnResult = string.Empty;
        var reservedScpTicketsContent = string.Empty;
        
        Logger.Debug("Setting SCP roles ...", Core.Singleton.Config.Debug);
        
        foreach (var kvp in predictedSpawnRoles.Where(x => x.Value.IsAlive() && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost && (x.Key.IsPlayer || Core.Singleton.Config.Dummy)))
        {
            var player = kvp.Key;
            var roleType = kvp.Value;

            var tickets = 0;

            if (ticketsEntries.TryGetValue(player.UserId, out var ticketEntry))
                tickets = ticketEntry.Tickets;
            
            spawnResult += $"- [{player.PlayerId}] **{roleType}** / {tickets}\n  {spawnReason[player]}\n";
            
            if (Core.Singleton.ShouldModifyTickets)
                reservedScpTicketsContent += $"{player.UserId};{tickets}\n";

            Core.Singleton.ResetTickets.Add(player.UserId);
            
            player.SetRole(roleType, RoleChangeReason.RoundStart);
            
            player.SendConsoleMessage($"[FairScpSelection]\nYou were spawned as {roleType}\nReason: {spawnReason[player]}", "white");
        }

        if (!Core.Singleton.Config.ReservedScpTickets.IsEnabled) 
            return false;
        
        Logger.Debug("Saving ReservedScpTickets.txt ...", Core.Singleton.Config.Debug);
            
        Core.Singleton.WriteReservedScpTicketsFile(reservedScpTicketsContent);

        return false;
    }
}