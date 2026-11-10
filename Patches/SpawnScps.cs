using System.Linq;
using LabApi.Features.Extensions;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace FairScpSelection.Patches;

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

        RoleAssigner.ScpOverflowMaxHsMultiplier = 1f;
        RoleAssigner.ScpsOverflowing = false;
        
        var minTickets = Mathf.Max(Core.Singleton.Config.TicketsDistribution.MinTickets, 0);
        var maxTickets = Mathf.Max(Core.Singleton.Config.TicketsDistribution.MaxTickets, 0);

        var playersCount = Extensions.GetPlayersCount();

        var targetScpNumber = Extensions.GetTargetRolesNumber(playersCount);
        
        var scpLimitNumber = Extensions.GetRolesLimitNumber(true);
        
        Logger.Debug($"Target roles number is set to {(Core.Singleton.TargetRolesNumber != -1 ? $"{targetScpNumber} via command" : targetScpNumber)} ...", Core.Singleton.Config.Debug);
        
        var ticketsEntries = Core.Singleton.TicketLoader.TicketsEntries;
        
        Logger.Debug($"Predicting spawn roles for {playersCount} player(-s) ...", Core.Singleton.Config.Debug);
        
        var predictedSpawnRoles = Extensions.PredictSpawnRoles(ticketsEntries, targetScpNumber, out var spawnReason);
        
        var spawnResult = string.Empty;
        var reservedScpTicketsContent = string.Empty;
        
        var spawnedPlayersCount = predictedSpawnRoles.Count(x =>
            x.Value != RoleTypeId.Destroyed && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost &&
            (x.Key.IsPlayer || Core.Singleton.Config.DummyMode));
        var spawnedScpsCount = predictedSpawnRoles.Count(x =>
            x.Value.IsScp() && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost &&
            (x.Key.IsPlayer || Core.Singleton.Config.DummyMode));
        
        var shouldCompensate = !Core.Singleton.AllowScpOverflow && targetScpNumber > scpLimitNumber && spawnedScpsCount > 0;

        if (shouldCompensate)
        {
            Logger.Debug($"Target SCP number exceeds the role limit. SCPs will receive a +{(targetScpNumber - scpLimitNumber) * 5}% bonus to their max HS and +10% to their max HP", Core.Singleton.Config.Debug);
            
            RoleAssigner.ScpOverflowMaxHsMultiplier = 1f + (targetScpNumber - scpLimitNumber) * 0.05f;
            RoleAssigner.ScpsOverflowing = true;
        }
        
        Logger.Debug($"Spawning {spawnedPlayersCount} of {targetScpNumber} target(-s) ...", Core.Singleton.Config.Debug);
        
        foreach (var kvp in predictedSpawnRoles.Where(x => x.Value != RoleTypeId.Destroyed && !x.Key.IsDestroyed && x.Key.IsReady && !x.Key.IsHost && (x.Key.IsPlayer || Core.Singleton.Config.DummyMode)))
        {
            var player = kvp.Key;
            var roleType = kvp.Value;

            var tickets = minTickets;

            if (ticketsEntries.TryGetValue(player.UserId, out var ticketEntry))
                tickets = Mathf.Clamp(ticketEntry.Tickets, minTickets, maxTickets);
            
            spawnResult += $"- [{player.PlayerId}] / {roleType} / {tickets} / {spawnReason[player]}\n";
            reservedScpTicketsContent += $"{player.UserId};{tickets}\n";

            Core.Singleton.ResetTickets.Add(player.UserId);
            
            player.SetRole(roleType, RoleChangeReason.RoundStart);
            
            player.SendConsoleMessage($"[FairScpSelection]\nYou were spawned as {Extensions.GetRoleTranslation(roleType)}\nReason: {spawnReason[player]}", "white");
        }

        Logger.Debug(string.IsNullOrEmpty(spawnResult) ? "No spawn results :(" : $"Spawn results:\n{spawnResult}", Core.Singleton.Config.Debug);
            
        Core.Singleton.WriteReservedScpTicketsFile(reservedScpTicketsContent);

        return false;
    }
}