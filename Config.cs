using System.Collections.Generic;
using System.ComponentModel;
using FairScpSelection.Configs;
using PlayerRoles;

namespace FairScpSelection;

public class Config
{
    [Description("Read config docs at https://github.com/CosmosZvezdo4kin/FairScpSelection")]
    public bool Debug { get; set; } = false;
    public bool Dummy { get; set; } = false;
    public int TicketsPerRound { get; set; } = 2;
    public int MinTickets { get; set; } = 10;
    public int MaxTickets { get; set; } = 32;
    public int MaxStorageTime { get; set; } = 1296000;
    public LobbyInterface LobbyInterface { get; set; } = new();
    public ReservedScpTickets ReservedScpTickets { get; set; } = new();
    public SpawnRestrictions SpawnRestrictions { get; set; } = new();
    public Dictionary<RoleTypeId, int> RoleLimitations { get; set; } = new()
    {
        { RoleTypeId.Scp049, 1 },
        { RoleTypeId.Scp079, 1 },
        { RoleTypeId.Scp096, 1 },
        { RoleTypeId.Scp106, 1 },
        { RoleTypeId.Scp173, 1 },
        { RoleTypeId.Scp939, 1 },
        { RoleTypeId.Scp3114, 1 }
    };
}