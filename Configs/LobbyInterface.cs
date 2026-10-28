using System.Collections.Generic;
using PlayerRoles;

namespace FairScpSelection.Configs;

public class LobbyInterface
{
    public bool IsEnabled { get; set; } = true;
    public float CoroutineDelay { get; set; } = 1f;
    public float HintDuration { get; set; } = 2f;
    public HashSet<RoleTypeId> ShowRolesTeammatesWhitelist { get; set; } = new();
    public HashSet<Team> ShowTeamTeammatesWhitelist { get; set; } = new() { Team.SCPs };
    public HashSet<Faction> ShowFactionTeammatesWhitelist { get; set; } = new();
}