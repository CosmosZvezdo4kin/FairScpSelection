using System.Linq;
using GameCore;
using HarmonyLib;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.RoleAssign;

namespace FairScpSelection.Patches;

[HarmonyPatch(typeof(ScpSpawner), nameof(ScpSpawner.SpawnScps))]
public class SpawnScps
{
    public static bool Prefix()
    {
        if (Core.Singleton == null)
            return true;

        if (Core.Singleton.TicketLoader == null || !Core.Singleton.TicketLoader.IsReady)
        {
            Logger.Error("TicketLoader is null or not ready");
            return true;
        }
        
        var teamRespawnQueue = ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");
        
        var playersCount =
            Player.List.Count(x => !x.IsDestroyed && x.IsReady && RoleAssigner.CheckPlayer(x.ReferenceHub) && (ScpSpawnPreferences.Preferences.ContainsKey(x.Connection.connectionId) || Core.Singleton.Config.IncludeDummyInCalculations));
        var targetScpNumber = Enumerable.Range(0, playersCount)
            .Count(i => teamRespawnQueue[i % teamRespawnQueue.Length] == '0');
        
        Logger.Debug($"Target SCP number is set to {(Core.Singleton.TargetRolesNumber != -1 ? $"{Core.Singleton.TargetRolesNumber} via command" : targetScpNumber)} ...", Core.Singleton.Config.Debug);
        
        var ticketsEntries = Core.Singleton.TicketLoader.TicketsEntries;
        
        Logger.Debug($"Predicting spawn roles for {playersCount} player(-s) ...", Core.Singleton.Config.Debug);
        
        var predictedSpawnRoles = Core.Singleton.PredictSpawnRoles(ticketsEntries, targetScpNumber, out var spawnReason);
        
        var spawnResult = string.Empty;
        var reservedScpTicketsContent = string.Empty;
        
        var spawnedPlayersCount = predictedSpawnRoles.Count(x =>
            x.Value != RoleTypeId.Destroyed && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost &&
            (x.Key.IsPlayer || Core.Singleton.Config.DummyMode));
        
        Logger.Debug($"Spawning {spawnedPlayersCount} of {targetScpNumber} target(-s) ...", Core.Singleton.Config.Debug);
        
        foreach (var kvp in predictedSpawnRoles.Where(x => x.Value != RoleTypeId.Destroyed && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost && (x.Key.IsPlayer || Core.Singleton.Config.DummyMode)))
        {
            var player = kvp.Key;
            var roleType = kvp.Value;

            var tickets = 0;

            if (ticketsEntries.TryGetValue(player.UserId, out var ticketEntry))
                tickets = ticketEntry.Tickets;
            
            spawnResult += $"- [{player.PlayerId}] / {roleType} / {tickets} / {spawnReason[player]}\n";
            
            if (Core.Singleton.ShouldModifyTickets)
                reservedScpTicketsContent += $"{player.UserId};{tickets}\n";

            Core.Singleton.ResetTickets.Add(player.UserId);
            
            player.SetRole(roleType, RoleChangeReason.RoundStart);
            
            player.SendConsoleMessage($"[FairScpSelection]\nYou were spawned as {roleType}\nReason: {spawnReason[player]}", "white");
        }

        Logger.Debug(string.IsNullOrEmpty(spawnResult) ? "No spawn results :(" : $"Spawn results:\n{spawnResult}", Core.Singleton.Config.Debug);

        if (!Core.Singleton.Config.ReservedScpTickets.IsEnabled) 
            return false;
        
        Logger.Debug("Saving ReservedScpTickets.txt ...", Core.Singleton.Config.Debug);
            
        Core.Singleton.WriteReservedScpTicketsFile(reservedScpTicketsContent);

        return false;
    }
}