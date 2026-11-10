namespace FairScpSelection.Configs;

public class TicketsDistribution
{
    public int MinTickets { get; set; } = 10;
    public int MaxTickets { get; set; } = 32;
    public int TicketsPerRound { get; set; } = 2;
    public int MaxStorageTime { get; set; } = 1296000;
    public float MaxRoundDuration { get; set; } = 0f;
}