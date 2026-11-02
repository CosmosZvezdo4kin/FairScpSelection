using System.Collections.Generic;
using System.Linq;
using FairScpSelection.Structures;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using MapGeneration.Holidays;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.RoleAssign;
using UnityEngine;

namespace FairScpSelection;

public static class Extensions
{
    public static Dictionary<Player, RoleTypeId> PredictSpawnRoles(Dictionary<string, TicketEntry> entries, int targetRolesNumber, out Dictionary<Player, string> spawnReason)
    {
        var predictedRoles = new Dictionary<Player, RoleTypeId>();
        spawnReason = new Dictionary<Player, string>();

        if (Core.Singleton == null)
            return predictedRoles;
        
        if (Core.Singleton.TargetRolesNumber >= 0)
            targetRolesNumber = Core.Singleton.TargetRolesNumber;
        
        var minTickets = Mathf.Clamp(Core.Singleton.Config.MinTickets, 0, int.MaxValue);
        
        var playerTickets = new Dictionary<Player, int>();
        
        var roleLimitations = new Dictionary<RoleTypeId, int>(Core.Singleton.RoleLimitations);

        if (targetRolesNumber == 0 || roleLimitations.Count(x => x.Value > 0) == 0)
            return predictedRoles;
        
        foreach (var player in Player.List.Where(x => x.HasSpawnPreferences()))
        {
            if (!RoleAssigner.CheckPlayer(player.ReferenceHub))
            {
                predictedRoles[player] = RoleTypeId.Destroyed;
                spawnReason[player] = Core.Singleton.Translation.UnavailableRoleSpawnReason;
                continue;
            }
            
            var tickets = minTickets;

            if (entries.TryGetValue(player.UserId, out var ticketsEntry))
                tickets = ticketsEntry.Tickets;

            playerTickets[player] = tickets;
        }

        if (!playerTickets.Any())
            return predictedRoles;

        var iteration = 0;
        var targetsCount = 0;

        while (targetsCount < targetRolesNumber && roleLimitations.Count(x => x.Value > 0) > 0 && iteration < 2)
        {
            var playerList = Core.Singleton.ShouldOrderListByTickets ? playerTickets.OrderByDescending(x => x.Value).ToList() : playerTickets.OrderByDescending(_ => Random.Range(0, 2) < 1).ToList();
            
            foreach (var player in playerList.Select(x => x.Key))
            {
                if (targetsCount >= targetRolesNumber || roleLimitations.Count(x => x.Value > 0) == 0)
                    break;
                
                if (predictedRoles.ContainsKey(player))
                    continue;

                var spawnPreferences = player.GetSpawnPreferences();
                
                var preferences = spawnPreferences.Preferences
                    .Select(p => new KeyValuePair<RoleTypeId, int>(
                        p.Key,
                        p.Key.CanSpawn() && Core.Singleton.RoleLimitations.TryGetValue(p.Key, out var limit) && limit > 0 ? p.Value + 5 : 0))
                    .Concat(
                        Core.Singleton.RoleLimitations
                            .Where(kvp => kvp.Key.CanSpawn() && kvp.Value > 0)
                            .Select(kvp => kvp.Key)
                            .Except(spawnPreferences.Preferences.Select(p => p.Key))
                            .Select(key => new KeyValuePair<RoleTypeId, int>(key, 0)))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                if (player.IsPlayer && iteration == 0 && ScpPlayerPicker.IsOptedOutOfScp(player.ReferenceHub))
                    continue;
                
                var allPreferencesBelowMid = preferences.All(x => x.Value < 5);
                
                foreach (var kvp in preferences.OrderByDescending(x => x.Value))
                {
                    var roleType = kvp.Key;
                    var preferenceValue = kvp.Value;
                    
                    if (!roleType.CanQueueRole(predictedRoles, Mathf.Min(targetRolesNumber, playerTickets.Count())))
                        continue;
                
                    if (!roleLimitations.TryGetValue(roleType, out var limitNumber) || limitNumber <= 0)
                        continue;
                    
                    if (!allPreferencesBelowMid && preferenceValue < 5 && iteration == 0)
                        continue;

                    roleLimitations[roleType] -= 1;
                    targetsCount++;
                
                    predictedRoles[player] = roleType;
                    spawnReason[player] = iteration == 0 ? string.Format(Core.Singleton.Translation.HighPreferenceSpawnReason, preferenceValue) : string.Format(Core.Singleton.Translation.NotEnoughPlayersSpawnReason, preferenceValue);
                
                    break;
                }
            }
            
            iteration++;
        }

        return predictedRoles;
    }
    public static bool CanSpawn(this RoleTypeId roleTypeId)
    {
        if (Core.Singleton == null)
            return false;
        
        if (!Core.Singleton.Config.SpawnRestrictions.IsEnabled)
            return true;
        
        if (Core.Singleton.Config.SpawnRestrictions.PreventNonScp && !roleTypeId.IsScp())
            return false;

        if (Core.Singleton.Config.SpawnRestrictions.PreventZombies && roleTypeId == RoleTypeId.Scp0492)
            return false;

        if (Core.Singleton.Config.SpawnRestrictions.Scp3114HolidaysOnly && roleTypeId == RoleTypeId.Scp3114 && !Scp3114Role.SupportedHolidays.IsAnyHolidayActive())
            return false;

        return true;
    }

    public static bool CanQueueRole(this RoleTypeId roleTypeId, Dictionary<Player, RoleTypeId> enqueuedRoles, int potentialCandidatesCount)
    {
        if (Core.Singleton == null)
            return false;
        
        if (!Core.Singleton.Config.SpawnRestrictions.IsEnabled)
            return true;
        
        if (!roleTypeId.CanSpawn())
            return false;
        
        // potentialCandidates prevents certain roles from appearing unexpectedly (especially when the `include_dummy_in_calculations` config is enabled)
        
        if (Core.Singleton.Config.SpawnRestrictions.PreventAiLoneliness && roleTypeId == RoleTypeId.Scp079 && potentialCandidatesCount < 2)
            return false;
        
        if (Core.Singleton.Config.SpawnRestrictions.PreventShyGuyLoneliness && roleTypeId == RoleTypeId.Scp096 && potentialCandidatesCount < 2)
            return false;
            
        if (Core.Singleton.Config.SpawnRestrictions.PreventWorstDuo && roleTypeId == RoleTypeId.Scp096 && enqueuedRoles.ContainsValue(RoleTypeId.Scp079) && potentialCandidatesCount < 3)
            return false;
            
        if (Core.Singleton.Config.SpawnRestrictions.PreventWorstDuo && roleTypeId == RoleTypeId.Scp079 && enqueuedRoles.ContainsValue(RoleTypeId.Scp096) && potentialCandidatesCount < 3)
            return false;

        return true;
    }
    
    public static bool HasSpawnPreferences(this Player player)
    {
        if (Core.Singleton == null)
            return false;

        if (player == null || player.IsDestroyed || !player.IsReady || player.IsHost)
            return false;

        if (!player.IsPlayer && !Core.Singleton.Config.DummyMode)
            return false;
        
        if (player.IsDummy && !ScpSpawnPreferences.Preferences.ContainsKey(player.Connection.connectionId))
            ScpSpawnPreferences.Preferences[player.Connection.connectionId] = new ScpSpawnPreferences.SpawnPreferences(false);

        return ScpSpawnPreferences.Preferences.ContainsKey(player.Connection.connectionId);
    }

    public static ScpSpawnPreferences.SpawnPreferences GetSpawnPreferences(this Player player)
    {
        return !player.HasSpawnPreferences() ? default : ScpSpawnPreferences.Preferences[player.Connection.connectionId];
    }
}