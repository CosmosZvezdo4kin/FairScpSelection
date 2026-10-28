using System;
using System.Collections.Generic;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace FairScpSelection.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ModifyTickets : ICommand, IUsageProvider
{
    public string Command => "modifytickets";
    public string[] Aliases => new string[] { "modtickets" };
    public string Description => "Change SCP tickets number for player(-s)";
    public string[] Usage => new string[] { "PlayerId or PlayerId.PlayerId...", "Count" };
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

        if (Core.Singleton.TicketLoader == null)
        {
            response = "TicketLoader is null";
            return false;
        }

        if (Core.Singleton.TicketLoader.IsBusy || !Core.Singleton.TicketLoader.IsReady)
        {
            response = "TicketsLoader is not ready or busy";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Not enough arguments\n" + this.DisplayCommandUsage();
            return false;
        }
        
        var stringPlayerIds = arguments.At(0).Split('.');
        var playerList = new HashSet<Player>();

        foreach (var stringPlayerId in stringPlayerIds)
        {
            if (!int.TryParse(stringPlayerId, out var playerId))
            {
                response = $"Valid PlayerId was not provided - {stringPlayerId}\n" + this.DisplayCommandUsage();
                return false;
            }

            if (!Player.TryGet(playerId, out var player) || player.IsDestroyed || !player.IsReady || player.IsHost || (!player.IsPlayer && !Core.Singleton.Config.DummyMode))
            {
                response = $"Player with this PlayerId was not found - {stringPlayerId}\n" + this.DisplayCommandUsage();
                return false;
            }
            
            playerList.Add(player);
        }
        
        var minTickets = Mathf.Clamp(Core.Singleton.Config.MinTickets, 0, int.MaxValue);
        var maxTickets = Mathf.Clamp(Core.Singleton.Config.MaxTickets, 0, int.MaxValue);

        if (!int.TryParse(arguments.At(1), out var tickets) || tickets < 0)
        {
            response = "Incorrect value for the number of SCP tickets (must be an integer and no less than 0)\n" + this.DisplayCommandUsage();
            return false;
        }

        if (tickets < minTickets)
        {
            response = $"Incorrect value for the number of SCP tickets (must be at least {minTickets})\n" + this.DisplayCommandUsage();
            return false;
        }
        
        if (tickets > maxTickets)
        {
            response = $"Incorrect value for the number of SCP tickets (must be no greater than 0 {maxTickets})\n" + this.DisplayCommandUsage();
            return false;
        }
        
        foreach (var player in playerList)
        {
            var prevTickets = 0;
            
            if (Core.Singleton.TicketLoader.TicketsEntries.TryGetValue(player.UserId, out var ticketEntry))
                prevTickets = ticketEntry.Tickets;
                
            Core.Singleton.TicketLoader.ModifyTickets(player.UserId, tickets);
                
            player.SendConsoleMessage($"[FairScpSelection]\nThe number of tickets has been changed by an admin command ({prevTickets} -> {tickets})", "white");
        }

        Core.Singleton.TicketLoader.SaveEntries();

        response = "SCP tickets have been successfully modified";
        return true;
    }
}