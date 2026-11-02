using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FairScpSelection.Patches;
using FairScpSelection.Structures;
using GameCore;
using HarmonyLib;
using Hints;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;
using Random = UnityEngine.Random;
using Version = System.Version;

namespace FairScpSelection;

public class Core : Plugin
{
    public override string Name => "FairScpSelection";
    public override string Description => "Reworked SCP selection system that prioritizes player preferences";
    public override string Author => "CosmosZvezdochkin";
    public override Version Version => new(1, 1, 1);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    
    public static Core Singleton;

    public TicketLoader TicketLoader;
    internal Config Config;
    internal Translation Translation;
    
    #if YASSA
    
    #endif
    public Dictionary<RoleTypeId, int> RoleLimitations { get; } = new();
    public HashSet<string> ResetTickets { get; } = new();
    public int TargetRolesNumber { get; set; } = -1;
    public bool ShouldModifyTickets { get; set; } = true;
    public bool ShouldOrderListByTickets { get; set; } = true;
    public bool AllowScpOverflow { get; set; } = false;
    
    private bool _hasValidConfigs;
    private bool _hasValidTranslation;
    private Dictionary<string, int> _reservedScpTickets;
    private Dictionary<Player, RoleTypeId> _predictedRoles;
    private Dictionary<Player, string> _spawnReasons;
    private HashSet<string> _modifiedTickets;
    private CoroutineHandle _ticketsCoroutine;
    private CoroutineHandle _lobbyUiCoroutine;
    private Harmony _harmony;
    
    private static string ReservedScpTicketsPath => Path.Combine(FileManager.GetAppFolder(true, true, string.Empty), "ReservedScpTickets.txt");
    
    public override void Enable()
    {
        if (!_hasValidConfigs)
        {
            Logger.Error("Invalid config file!");
            return;
        }

        if (!_hasValidTranslation)
        {
            Logger.Error("Invalid translation file!");
            return;
        }

        if (!PatchMethods())
            return;
        
        TicketLoader ??= new TicketLoader(Config.MaxStorageTime);
        
        ResetRoleLimitations();
        
        _reservedScpTickets = new Dictionary<string, int>();
        _predictedRoles = new Dictionary<Player, RoleTypeId>();
        _spawnReasons =  new Dictionary<Player, string>();
        _modifiedTickets = new HashSet<string>();
        
        AllowScpOverflow = ConfigFile.ServerConfig.GetBool("allow_scp_overflow", false);

        RegisterEvents();
        
        Singleton = this;
    }

    public override void Disable()
    {
        UnpatchMethods();
        UnregisterEvents();
        Dispose();
        
        Singleton = null;
        
        TicketLoader = null;
        Config = null;
        Translation = null;
    }

    public override void LoadConfigs()
    {
        base.LoadConfigs();
        
        _hasValidConfigs = this.TryLoadConfig("config.yml", out Config);
        _hasValidTranslation = this.TryLoadConfig("translation.yml", out Translation);
    }

    #region PLUGIN UTILITY
    
    private string FormatUiText(string input, Player player)
    {
        var tickets = Translation.LobbyUiNoData;

        if (TicketLoader.TicketsEntries.TryGetValue(player.UserId, out var ticketsEntry))
            tickets = ticketsEntry.Tickets.ToString();
        
        var predictedRole = _predictedRoles.TryGetValue(player, out var role) ? role : RoleTypeId.Destroyed;
        
        var bufferTeammates = new Dictionary<RoleTypeId, int>();
        
        var predictedResult = string.Empty;
        var predictedTeammates = Translation.LobbyUiNoData;

        var canSeeTeammates = Config.LobbyInterface.ShowRolesTeammatesWhitelist.Contains(predictedRole) ||
                              Config.LobbyInterface.ShowTeamTeammatesWhitelist.Contains(predictedRole.GetTeam()) ||
                              Config.LobbyInterface.ShowFactionTeammatesWhitelist.Contains(predictedRole.GetFaction());

        if (predictedRole == RoleTypeId.Destroyed)
        {
            predictedResult = Translation.LobbyUiOtherRole;
        }
        else if (!predictedRole.IsAlive())
        {
            predictedResult = Translation.LobbyUiUnavailableRole;
        }
        else if (predictedRole.IsAlive())
        {
            predictedResult = predictedRole.ToString();
            
            if (canSeeTeammates && _predictedRoles.Count(kvp => kvp.Value.IsAlive()) > 1)
            {
                foreach (var roleType in _predictedRoles.Where(kvp => kvp.Value.IsAlive() && kvp.Key != player && kvp.Value.GetFaction() == predictedRole.GetFaction()).Select(kvp => kvp.Value))
                {
                    if (!bufferTeammates.ContainsKey(roleType))
                        bufferTeammates[roleType] = 0;
                    
                    bufferTeammates[roleType]++;
                }
                
                predictedTeammates = string.Join(", ", bufferTeammates.Select(kvp => $"{kvp.Key} {(kvp.Value > 1 ? $"(x{kvp.Value})" : string.Empty)}"));
            }
        }
        
        return input
            .Replace("{ScpTickets}", tickets)
            .Replace("{PredictedRole}", predictedResult)
            .Replace("{PredictedTeammates}", predictedTeammates)
            .Replace("{SpawnReason}", _spawnReasons.TryGetValue(player, out var spawnReason) ? spawnReason : string.Empty);
    }
    
    #endregion

    #region GENERAL UTILITY
    
    private void Dispose()
    {
        TargetRolesNumber = -1;
        ShouldModifyTickets = true;
        ShouldOrderListByTickets = true;
        AllowScpOverflow = ConfigFile.ServerConfig.GetBool("allow_scp_overflow", false);
        
        ResetTickets.Clear();
        
        _predictedRoles.Clear();
        _spawnReasons.Clear();
        _reservedScpTickets.Clear();
        _modifiedTickets.Clear();
        
        ResetRoleLimitations();

        if (_ticketsCoroutine.IsRunning)
            Timing.KillCoroutines(_ticketsCoroutine);
        
        if (_lobbyUiCoroutine.IsRunning)
            Timing.KillCoroutines(_lobbyUiCoroutine);
    }

    private void RegisterEvents()
    {
        ServerEvents.RoundRestarted += OnRoundRestarted;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RoundEnded += OnRoundEnded;
    }

    private void UnregisterEvents()
    {
        ServerEvents.RoundRestarted -= OnRoundRestarted;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RoundEnded -= OnRoundEnded;
    }

    private bool PatchMethods()
    {
        _harmony ??= new Harmony($"{Author}.{Name}.{DateTime.Now.Ticks}");

        try
        {
            _harmony.Patch(
                original: AccessTools.Method(typeof(ScpSpawner), nameof(ScpSpawner.SpawnScps)),
                prefix: new HarmonyMethod(typeof(SpawnScps), nameof(SpawnScps.Prefix)));
            
            //_harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error occured while patching methods:\n{ex}");
            return false;
        }
        
        Logger.Info($"Successfully patched {_harmony.GetPatchedMethods()?.Count() ?? 0} method(-s)");
        return true;
    }

    private void UnpatchMethods()
    {
        _harmony?.UnpatchAll();

        _harmony = null;
    }
    
    private void ResetRoleLimitations()
    {
        RoleLimitations.Clear();
        
        foreach (var configRoleLimitation in Config.RoleLimitations)
            RoleLimitations[configRoleLimitation.Key] = configRoleLimitation.Value;
    }
    
    #endregion

    #region ROUND EVENTS

    private void OnRoundRestarted()
    {
        Dispose();
    }

    private void OnWaitingForPlayers()
    {
        if (Config.ReservedScpTickets.IsEnabled)
        {
            ReadReservedScpTicketsFile();
        
            WriteReservedScpTicketsFile(string.Empty);
        }
        
        if (!_ticketsCoroutine.IsRunning)
            _ticketsCoroutine = Timing.RunCoroutine(GiveTickets());

        if (Config.LobbyInterface.IsEnabled && !_lobbyUiCoroutine.IsRunning)
            _lobbyUiCoroutine = Timing.RunCoroutine(RunLobbyUi());
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        if (!Config.ReservedScpTickets.IsEnabled)
            return;
        
        if (Config.ReservedScpTickets.MinRoundDuration > 0 && Round.Duration.TotalSeconds < Config.ReservedScpTickets.MinRoundDuration)
            return;
        
        WriteReservedScpTicketsFile(string.Empty);
    }
    
    #endregion

    #region RESERVED SCP TICKETS
    
    internal void WriteReservedScpTicketsFile(string content)
    {
        File.WriteAllText(ReservedScpTicketsPath, content);
    }
    
    private void ReadReservedScpTicketsFile()
    {
        if (!File.Exists(ReservedScpTicketsPath))
        {
            WriteReservedScpTicketsFile(string.Empty);
            return;
        }

        var textLines = File.ReadAllLines(ReservedScpTicketsPath);

        foreach (var line in textLines.Where(x => !string.IsNullOrEmpty(x.Trim())))
        {
            var arguments = line.Split(';');
            
            if (arguments.Length != 2)
                continue;
            
            var userId = arguments[0];
            
            if (!int.TryParse(arguments[1], out var tickets))
                continue;
            
            _reservedScpTickets[userId] = tickets;
        }
    }
    
    #endregion

    #region COROUTINES
    
    private IEnumerator<float> GiveTickets()
    {
        var ticketsPerRound = Mathf.Clamp(Config.TicketsPerRound, 0, int.MaxValue);
        var minTickets = Mathf.Clamp(Config.MinTickets, 0, int.MaxValue);
        var maxTickets = Mathf.Clamp(Config.MaxTickets, 0, int.MaxValue);
        
        while (RoundSummary.singleton == null)
            yield return Timing.WaitForOneFrame;

        while (!Round.IsRoundStarted && !Round.IsRoundEnded)
        {
            if (!_reservedScpTickets.Any() || TicketLoader == null || !TicketLoader.IsReady || TicketLoader.IsBusy)
            {
                yield return Timing.WaitForOneFrame;
                continue;
            }

            foreach (var kvp in _reservedScpTickets)
                TicketLoader.ModifyTickets(kvp.Key, Mathf.Clamp(kvp.Value + ticketsPerRound, minTickets + ticketsPerRound, maxTickets));
            
            TicketLoader.SaveEntries();
            
            Logger.Debug($"Restored SCP tickets for {_reservedScpTickets.Count} player(s)", Config.Debug);
            
            _reservedScpTickets.Clear();
        }
        
        RoundIsInProgress:
        while (Round.IsRoundInProgress)
        {
            if (TicketLoader == null || !TicketLoader.IsReady || TicketLoader.IsBusy || !ShouldModifyTickets)
            {
                yield return Timing.WaitForOneFrame;
                continue;
            }
            
            var hasChanges = false;
                
            foreach (var player in Player.List.Where(x => !x.IsDestroyed && x.IsReady && !x.IsHost && (x.IsPlayer || Config.DummyMode)))
            {
                if (!_modifiedTickets.Add(player.UserId))
                    continue;

                var tickets = minTickets;
                
                if (TicketLoader.TicketsEntries.TryGetValue(player.UserId, out var ticketEntry))
                    tickets = ticketEntry.Tickets;
                
                var prevTickets = tickets;

                if (ResetTickets.Remove(player.UserId) || tickets < minTickets)
                {
                    tickets = minTickets;
                } 
                else if (tickets > maxTickets)
                {
                    tickets = maxTickets;
                }
                else
                {
                    tickets += ticketsPerRound;
                }

                TicketLoader.ModifyTickets(player.UserId, tickets);
                
                hasChanges = true;
                
                player.SendConsoleMessage($"[FairScpSelection]\nThe number of your SCP tickets has been changed ({prevTickets} -> {tickets})", "white");
            }
            
            if (hasChanges)
                TicketLoader.SaveEntries();
            
            yield return Timing.WaitForSeconds(10);
        }

        while (Round.IsRoundEnded)
            yield return Timing.WaitForOneFrame;
        
        goto RoundIsInProgress;
    }
    
    private IEnumerator<float> RunLobbyUi()
    {
        var teamRespawnQueue = ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");
        var coroutineDelay = Mathf.Clamp(Config.LobbyInterface.CoroutineDelay, 0, float.MaxValue);
        var hintDuration = Mathf.Clamp(Config.LobbyInterface.HintDuration, 0.1f, float.MaxValue);
        
        while (RoundSummary.singleton == null)
            yield return Timing.WaitForOneFrame;

        while (!Round.IsRoundStarted)
        {
            if (TicketLoader == null || !TicketLoader.IsReady || TicketLoader.IsBusy)
            {
                yield return Timing.WaitForOneFrame;
                continue;
            }
            
            _predictedRoles.Clear();
            _spawnReasons.Clear();

            var playersCount =
                Player.List.Count(x =>
                    !x.IsDestroyed && x.IsReady && RoleAssigner.CheckPlayer(x.ReferenceHub) && (x.HasSpawnPreferences() || (x.IsDummy && Config.IncludeDummyInCalculations)));
            
            var targetScpNumber = Enumerable.Range(0, playersCount)
                .Count(i => teamRespawnQueue[i % teamRespawnQueue.Length] == '0');
            
            var predictedRoles = Extensions.PredictSpawnRoles(TicketLoader.TicketsEntries, targetScpNumber, out var spawnReason);
                
            foreach (var kvp in predictedRoles)
                _predictedRoles[kvp.Key] = kvp.Value;
            
            foreach (var kvp in spawnReason)
                _spawnReasons[kvp.Key] = kvp.Value;
            
            // FadeInAndOut(1,1,1) effect prevents hint transparency bug
            
            foreach (var player in Player.List.Where(x => !x.IsDestroyed && x.IsReady && x.IsPlayer))
                player.SendHint(FormatUiText(Translation.LobbyUiHintText, player), HintEffectPresets.FadeInAndOut(1, 1, 1), hintDuration);
            
            if (coroutineDelay > 0)
                yield return Timing.WaitForSeconds(coroutineDelay);
            else
                yield return Timing.WaitForOneFrame;
        }
    }
    
    #endregion
}