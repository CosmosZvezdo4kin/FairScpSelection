using System.Collections.Generic;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using MapGeneration.Holidays;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.RoleAssign;

namespace FairScpSelection;

public static class Extensions
{
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

    public static bool CanQueueRole(this RoleTypeId roleTypeId, Dictionary<Player, RoleTypeId> enqueuedRoles, int targetScpNumber, int potentialCandidatesCount)
    {
        if (Core.Singleton == null)
            return false;
        
        if (!Core.Singleton.Config.SpawnRestrictions.IsEnabled)
            return true;
        
        if (!roleTypeId.CanSpawn())
            return false;
        
        // potentialCandidates prevents certain roles from appearing unexpectedly (especially when the `include_dummy_in_calculations` config is enabled)
        
        if (Core.Singleton.Config.SpawnRestrictions.PreventAiLoneliness && roleTypeId == RoleTypeId.Scp079 && (targetScpNumber < 2 || potentialCandidatesCount < 2))
            return false;
        
        if (Core.Singleton.Config.SpawnRestrictions.PreventShyGuyLoneliness && roleTypeId == RoleTypeId.Scp096 && (targetScpNumber < 2 || potentialCandidatesCount < 2))
            return false;
            
        if (Core.Singleton.Config.SpawnRestrictions.PreventWorstDuo && roleTypeId == RoleTypeId.Scp096 && enqueuedRoles.ContainsValue(RoleTypeId.Scp079) && (targetScpNumber < 3 || potentialCandidatesCount < 3))
            return false;
            
        if (Core.Singleton.Config.SpawnRestrictions.PreventWorstDuo && roleTypeId == RoleTypeId.Scp079 && enqueuedRoles.ContainsValue(RoleTypeId.Scp096) && (targetScpNumber < 3 || potentialCandidatesCount < 3))
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