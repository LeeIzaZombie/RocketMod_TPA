using Rocket.Unturned;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RocketMod_TPA
{
    public class CommandTPA : IRocketCommand
    {
        #region Delcarations
        public bool RunFromConsole
        {
            get { return false; }
        }

        public string Name
        {
            get { return "tpa"; }
        }

        public string Syntax
        {
            get
            {
                return "tpa (player/accept/deny/ignore)";
            }
        }

        public string Help
        {
            get { return "Request a teleport to a player, accept or deny other requests."; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        Dictionary<Steamworks.CSteamID, Steamworks.CSteamID> requests = new Dictionary<Steamworks.CSteamID, Steamworks.CSteamID>();
        #endregion

        public void Execute(RocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                RocketChat.Say(caller, "TPA allows you to request a teleport to another player.", Color.yellow);
                RocketChat.Say(caller, "/tpa (playerName) - Sends a teleport request.", Color.yellow);
                RocketChat.Say(caller, "/tpa accept - Accepts your latest TPA request.", Color.yellow);
                RocketChat.Say(caller, "/tpa deny - Denys your latest TPA request.", Color.yellow);
                return;
            }

            if (command[0].ToString().ToLower() == "accept" || command[0].ToString().ToLower() == "a" || command[0].ToString().ToLower() == "yes")
            {
                if (requests.ContainsKey(caller.CSteamID))
                {
                    RocketPlayer tpP = RocketPlayer.FromCSteamID(requests[caller.CSteamID]);
                    tpP.Teleport(caller);
                    requests.Remove(caller.CSteamID);
                    RocketChat.Say(caller, "You have accepted " + tpP.CharacterName + "'s tpa request!", Color.yellow);
                    RocketChat.Say(tpP, caller.CharacterName + " has accepted your tpa request!", Color.yellow);
                }
                else
                {
                    RocketChat.Say(caller, "Error: You don't have any tpa requests!", Color.red);
                }
            }
            else if (command[0].ToString() == "deny" || command[0].ToString().ToLower() == "d" || command[0].ToString().ToLower() == "no")
            {
                if (requests.ContainsKey(caller.CSteamID))
                {
                    RocketPlayer tpP = RocketPlayer.FromCSteamID(requests[caller.CSteamID]);
                    requests.Remove(caller.CSteamID);
                    RocketChat.Say(caller, "You have denied " + tpP.CharacterName + "'s tpa request!", Color.yellow);
                    RocketChat.Say(tpP, caller.CharacterName + " has denied your tpa request!", Color.red);
                }
                else
                {
                    RocketChat.Say(caller, "Error: You don't have any tpa requests!", Color.red);
                }
            }
            else //Try sending a tpa request to a player.
            {
                RocketPlayer rTo = RocketPlayer.FromName(command[0].ToString());

                #region Error Checking
                if (rTo == null)
                {
                    RocketChat.Say(caller, "Error: Could not find player!", Color.red);
                    return;
                }
                //Need to prevent spam requests.
                if (requests.ContainsKey(rTo.CSteamID))
                {
                    if (requests[rTo.CSteamID] == caller.CSteamID)
                    {
                        RocketChat.Say(caller, "Error: You already have a request pending to " + rTo.CharacterName, Color.red);
                        return;
                    }
                }
                #endregion

                if (requests.ContainsKey(rTo.CSteamID))
                {
                    requests[rTo.CSteamID] = caller.CSteamID;
                    RocketChat.Say(caller, "You have sent a tpa request to " + rTo.CharacterName, Color.yellow);
                    RocketChat.Say(rTo, caller.CharacterName + " has sent you a tpa request! use /tpa accept or /tpa deny", Color.yellow);
                }
                else
                {
                    requests.Add(rTo.CSteamID, caller.CSteamID);
                    RocketChat.Say(caller, "You have sent a tpa request to " + rTo.CharacterName, Color.yellow);
                    RocketChat.Say(rTo, caller.CharacterName + " has sent you a tpa request! use /tpa accept if you want them to teleport to you!", Color.yellow);
                }
            }

        }
    }
}
