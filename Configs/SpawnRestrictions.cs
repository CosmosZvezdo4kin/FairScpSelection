namespace FairScpSelection.Configs;

public class SpawnRestrictions
{
    public bool IsEnabled { get; set; } = true;
    public bool PreventNonScp { get; set; } = true;
    public bool PreventZombies { get; set; } = true;
    public bool PreventAiLoneliness { get; set; } = true;
    public bool PreventShyGuyLoneliness { get; set; } = true;
    public bool PreventWorstDuo { get; set; } = true;
    public bool RespectRolesPreferences { get; set; } = true;
}