# FairScpSelection
LabAPI plugin that reworks SCP selection system
___
# Installation
Put plugin dll from [releases](./releases/latest) in `LabAPI/plugins/...`

Put `0Harmony.dll` from [releases](./releases/latest) in `LabAPI/dependencies/...`
___
# Config

Config file is located at `LabAPI/configs/.../FairScpSelection/config.yml`

| Name                 | Description | Default value                                                                                    |
|----------------------|-------------|--------------------------------------------------------------------------------------------------|
| debug                |             | false                                                                                            |
| dummy                |             | false                                                                                            |
| tickets_per_round    |             | 2                                                                                                |
| min_tickets          |             | 10                                                                                               |
| max_tickets          |             | 32                                                                                               |
| max_storage_time     |             | 1296000                                                                                          |
| lobby_interface      |             | [See Here](#lobby-interface)                                                                     |
| reserved_scp_tickets |             | [See Here](#reserved-scp-tickets)                                                                |
| spawn_restrictions   |             | [See Here](#spawn-restrictions)                                                                  |
| role_limitations     |             | Scp049: 1<br/>Scp079: 1<br/>Scp096: 1<br/>Scp106: 1<br/>Scp173: 1<br/>Scp939: 1<br/>Scp3114: 1   |

## LobbyInterface

| Name                             | Description | Default value |
|----------------------------------|-------------|---------------|
| is_enabled                       |             | false         |
| coroutine_delay                  |             | 1             |
| hint_duration                    |             | 2             |
| show_roles_teammates_whitelist   |             | []            |
| show_team_teammates_whitelist    |             | [ SCPs ]      |
| show_faction_teammates_whitelist |             | []            |

## Reserved SCP tickets

| Name                           | Description | Default value |
|--------------------------------|-------------|---------------|
| is_enabled                     |             | true          |
| min_round_duration             |             | 60            |

## Spawn Restrictions

| Name                           | Description | Default value |
|--------------------------------|-------------|---------------|
| is_enabled                     |             | true          |
| prevent_non_scp                |             | true          |
| prevent_zombies                |             | true          |
| prevent_ai_loneliness          |             | true          |
| prevent_shy_guy_loneliness     |             | true          |
| prevent_worst_duo              |             | true          |
| scp3114_halloween_only         |             | true          |

___
# Translation
Translation file is located at `LabAPI/configs/.../FairScpSelection/config.yml`

| Name                               | Description | Default value                                                                                                                                                                                                                                                                                                                                                                                                                               |
|------------------------------------|-------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| unavailable_role_spawn_reason      |             | `Unavailable role`                                                                                                                                                                                                                                                                                                                                                                                                                          |
| high_preference_spawn_reason       |             | `High preference ({0})`                                                                                                                                                                                                                                                                                                                                                                                                                     |
| not_enough_players_spawn_reason    |             | `Not enough players ({0})`                                                                                                                                                                                                                                                                                                                                                                                                                  |
| lobby_ui_no_data                   |             | `???`                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| lobby_ui_unavailable_role          |             | `Mr. Nobody`                                                                                                                                                                                                                                                                                                                                                                                                                                |
| lobby_ui_other_role                |             | `Human`                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| lobby_ui_hint_text                 |             | `"<size=0>.</size><br><br><br><br><br><br><br><br><br><br><align=center><size=26><b><i><color=#e6ff00>\U0001F3AB</color> <color=#E05431>SCP Tickets: {ScpTickets}</color><br><color=#1c3c52>\U0001F3AD</color> <color=#76bf39>You will spawn as: {PredictedRole}<br><size=20><color=#588f2b>{SpawnReason}</color></size><br><color=#379bbf>\U0001F41B</color> <color=#bfad39>Teammates roles: {PredictedTeammates}</b></i></size></align>"` |