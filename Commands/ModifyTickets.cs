using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    public string[] Usage => new string[] { "PlayerId.PlayerId or UserId.UserId ...", "Count" };
    private string RequiredPermission => "fss." + Command;
    private static string UserIdPattern => @"^(\s+@northwood|\d{18}@discord|\d{17}@steam)$";
    
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
        var userIdList = new HashSet<string>();

        foreach (var stringPlayerId in stringPlayerIds)
        {
            if (int.TryParse(stringPlayerId, out var playerId))
            {
                if (!Player.TryGet(playerId, out var player) || player.IsDestroyed || !player.IsReady || player.IsHost || (!player.IsPlayer && !Core.Singleton.Config.DummyMode))
                {
                    response = $"Player with this PlayerId was not found - {stringPlayerId}\n" + this.DisplayCommandUsage();
                    return false;
                }

                userIdList.Add(player.UserId);
            }
            else if (stringPlayerId == "ID_Dummy" || Regex.IsMatch(stringPlayerId, UserIdPattern))
            {
                userIdList.Add(stringPlayerId);
            }
            else
            {
                response = $"Valid PlayerId/UserId was not provided - {stringPlayerId}\n" + this.DisplayCommandUsage();
                return false;
            }
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

        var modifiedCount = 0;
        
        foreach (var userId in userIdList)
        {
            var prevTickets = 0;
            
            if (Core.Singleton.TicketLoader.TicketsEntries.TryGetValue(userId, out var ticketEntry))
                prevTickets = ticketEntry.Tickets;
                
            if (!Core.Singleton.TicketLoader.TryModifyTickets(userId, tickets))
                continue;

            modifiedCount++;
            
            if (Player.TryGet(userId, out var player) && !player.IsDestroyed && player.IsReady && player.IsPlayer)
                player.SendConsoleMessage($"[FairScpSelection]\nThe number of tickets has been changed by an admin command ({prevTickets} -> {tickets})", "white");
        }

        if (modifiedCount < 1 || !Core.Singleton.TicketLoader.TrySaveEntries())
        {
            response = "An error occurred while attempting to modify SCP tickets (please try again later)";
            return false;
        }

        response = "SCP tickets have been successfully modified";
        return true;
    }
}