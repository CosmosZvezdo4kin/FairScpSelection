using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace FairScpSelection.Structures;

public class TicketLoader
{
    public Dictionary<string, TicketEntry> TicketsEntries { get; } = new();
    public bool IsBusy { get; private set; }
    public bool IsReady { get; private set; }
    private readonly long _maxStorageTime;
    
    private static readonly string FilePath = Path.Combine(FileManager.GetAppFolder(true, true, string.Empty), "ScpTickets.txt");

    public TicketLoader(int maxStorageTime)
    {
        IsBusy = false;
        IsReady = false;
        
        _maxStorageTime = Mathf.Clamp(maxStorageTime, 0, int.MaxValue);
        
        LoadEntries();
    }

    private void LoadEntries()
    {
        if (IsReady)
            return;
        
        IsBusy = true;

        if (!File.Exists(FilePath))
        {
            Logger.Warn($"File ScpTickets.txt not found! Creating a new one ({FilePath})");

            try
            {
                File.WriteAllText(FilePath, "");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create a new ScpTickets.txt file:\n{ex}");
                IsBusy = false;
                return;
            }
        }
        
        var unixTimeNow = DateTimeOffset.Now.ToUnixTimeSeconds();

        var lines = File.ReadAllLines(FilePath);

        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line) && TryDeserialize(line, out var ticketEntry) &&
                (_maxStorageTime == 0 || ticketEntry.LastUpdate + _maxStorageTime >= unixTimeNow))
                TicketsEntries[ticketEntry.UserId] = ticketEntry;
        }
        
        IsBusy = false;
        IsReady = true;
    }

    public void ModifyTickets(string userId, int newNumber)
    {
        if (IsBusy || !IsReady)
            return;
        
        IsBusy = true;
        
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        
        var ticketEntry = new TicketEntry()
        {
            UserId = userId,
            LastUpdate = now,
            Tickets = newNumber
        };

        TicketsEntries[userId] = ticketEntry;

        IsBusy = false;
    }

    public void SaveEntries()
    {
        if (IsBusy || !IsReady)
            return;
        
        IsBusy = true;
        
        FileManager.WriteToFile(TicketsEntries.Select(x => SerializeEntry(x.Value)), FilePath);
        
        IsBusy = false;
    }
    
    private static string SerializeEntry(TicketEntry entry)
    {
        return entry.UserId + ';' + entry.Tickets + ';' + entry.LastUpdate;
    }
    
    private static bool TryDeserialize(string text, out TicketEntry entry)
    {
        entry = null;
        
        if (string.IsNullOrEmpty(text))
            return false;
        
        var strArray = text.Split(';');
        
        if (strArray.Length != 3 || !int.TryParse(strArray[1], out var tickets) || !long.TryParse(strArray[2], out var lastUpdate))
            return false;
        
        entry = new TicketEntry()
        {
            UserId = strArray[0],
            Tickets = tickets,
            LastUpdate = lastUpdate,
        };
        
        return true;
    }
}