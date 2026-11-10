# Commands

List of plugin commands

| Command            | Description                                                                                                                                                                     | Arguments                                       | Required Permission     |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------|-------------------------|
| allowscpoverflow   | Allows enabling or disabling \"ScpOverflow\" compensation system. When it's disabled (false) SCPs will get bonus max HS and max HP if target SCP number exceeds the role limit  |                                                 | `fss.allowscpoverflow`  |
| modifytickets      | Change SCP tickets number for player(-s)                                                                                                                                        | (PlayerId.PlayerId or UserId.UserId...) (Count) | `fss.modifytickets`     |
| predictspawnroles  | It literally does what it says                                                                                                                                                  | (Count)                                         | `fss.predictspawnroles` |
| resetscpoverflow   | Completely resets the \"ScpOverflow\" compensation system bonus                                                                                                                 |                                                 | `fss.resetscpoverflow`  |
| rolelimitations    | Allows you to view (without arguments) or modify the limits on the number of times specific roles can appear in a round                                                         | (RoleType or RoleType1.RoleType2...)\<Count\>   | `fss.rolelimitations`   |
| sealscptickets     | Allows enabling or disabling the ability to modify tickets via internal systems (excluding commands)                                                                            |                                                 | `fss.sealscptickets`    |
| shufflespawn       | Allows enabling or disabling sorting by players' SCP tickets during role assignment                                                                                             |                                                 | `fss.shufflespawn`      |
| spawnpreference    | Allows you to view (with only one argument) or modify the specific role spawn preference for dummy (if dummy_mode is `true`)                                                    | (PlayerId)\<RoleType\> \<Count\>                | `fss.spawnpreference`   |
| targetrolesnumber  | Allows you to change the number of roles required for a spawn (default is -1)                                                                                                   | \<Count\>                                       | `fss.targetrolesnumber` |

> [!NOTE]
> After a round restart, all values modified by commands are restored to the values specified in the config (except `modifytickets` command)

### Example of usage

* `modifytickets 2.1 32`
* `modifytickets ID_Dummy 16`
* `modifytickets 761348728835907585@steam 20`
* `predictspawnroles 3`
* `rolelimitations Scp049.Scp096.Scp079.Scp106 0`
* `rolelimitations Scp3114 6` 💀