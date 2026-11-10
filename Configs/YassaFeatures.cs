using System.Collections.Generic;
using PlayerRoles;

namespace FairScpSelection.Configs;

public class YassaFeatures
{
    public bool IsRandomizeButtonEnabled { get; set; } = true;
    public HashSet<RoleTypeId> RolesPreferenceSliders { get; set; } = new() { RoleTypeId.Scp3114 };
}