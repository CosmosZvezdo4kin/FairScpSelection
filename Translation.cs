namespace FairScpSelection;

public class Translation
{
    public string UnavailableRoleSpawnReason { get; set; } = "Unavailable role";
    public string HighPreferenceSpawnReason { get; set; } = "High preference ({0})";
    public string NotEnoughPlayersSpawnReason { get; set; } = "Not enough players ({0})";
    public string LobbyUiNoData { get; set; } = "???";
    public string LobbyUiUnavailableRole { get; set; } = "Mr. Nobody";
    public string LobbyUiOtherRole { get; set; } = "Human";
    public string LobbyUiHintText { get; set; } =
        "<size=0>.</size><br><br><br><br><br><br><br><br><br><br><align=center><size=26><b><i><color=#e6ff00>\U0001F3AB</color> <color=#E05431>SCP Tickets: {ScpTickets}</color><br><color=#1c3c52>\U0001F3AD</color> <color=#76bf39>You will spawn as: {PredictedRole}<br><size=20><color=#588f2b>{SpawnReason}</color></size><br><color=#379bbf>\U0001F41B</color> <color=#bfad39>Teammates roles: {PredictedTeammates}</b></i></size></align>";
}