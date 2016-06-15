using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using SDG.Unturned;
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
        public bool AllowFromConsole
        {
            get
            {
                return false;
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { 
                    "CommandTPA.tpa"
                };
            }
        }

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
        Dictionary<Steamworks.CSteamID, DateTime> coolDown = new Dictionary<Steamworks.CSteamID, DateTime>();
        #endregion

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (command.Length < 1)
            {

                Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_1"), Color.yellow);
                Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_2"), Color.yellow);
                Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_3"), Color.yellow);
                Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_4"), Color.yellow);
                return;
            }

            if (command[0].ToString().ToLower() == "accept" || command[0].ToString().ToLower() == "a" || command[0].ToString().ToLower() == "yes")
            {

                if (!player.HasPermission("tpa.accept"))
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_accept"), Color.red);
                    return;
                }

                if (requests.ContainsKey(player.CSteamID))
                {
                    UnturnedPlayer tpP = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);
                    if (tpP.Stance == EPlayerStance.DRIVING || tpP.Stance == EPlayerStance.SITTING)
                    {
                        Rocket.Unturned.Chat.UnturnedChat.Say(tpP, PluginTPA.Instance.Translate("YouInCar"), Color.red);
                        Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("PlayerInCar"), Color.red);
                        requests.Remove(player.CSteamID);
                        return;
                    }
                    tpP.Teleport(player);
                    requests.Remove(player.CSteamID);
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_accepted") + " " + tpP.CharacterName, Color.yellow);
                    Rocket.Unturned.Chat.UnturnedChat.Say(tpP, player.CharacterName + " " + PluginTPA.Instance.Translate("request_accepted_1"), Color.yellow);
                }
                else
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                }
               
            }
            else if (command[0].ToString() == "deny" || command[0].ToString().ToLower() == "d" || command[0].ToString().ToLower() == "no")
            {
                if (!player.HasPermission("tpa.deny"))
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_deny"), Color.red);
                    return;
                }

                if (requests.ContainsKey(player.CSteamID))
                {
                    UnturnedPlayer tpP = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);
                    requests.Remove(player.CSteamID);
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_denied") + " " + tpP.CharacterName, Color.yellow);
                    Rocket.Unturned.Chat.UnturnedChat.Say(tpP, player.CharacterName + " " + PluginTPA.Instance.Translate("request_denied_1"), Color.red);
                }
                else
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                }
            }
            else //Try sending a tpa request to a player.
            {
                if (!player.HasPermission("tpa.send"))
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_send"), Color.red);
                    return;
                }
                

                UnturnedPlayer rTo = UnturnedPlayer.FromName(command[0].ToString());

                #region Error Checking
                if (rTo == null)
                {
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("playerNotFound"), Color.red);
                    return;
                }
                //Need to prevent spam requests.
                if (requests.ContainsKey(rTo.CSteamID))
                {
                    if (requests[rTo.CSteamID] == player.CSteamID)
                    {
                        Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_pending") + " " + rTo.CharacterName, Color.red);
                        return;
                    }
                }
                #endregion

                if (coolDown.ContainsKey(player.CSteamID))
                {
                    //Rocket.Unturned.Chat.UnturnedChat.Say(caller, "Debug: " + (DateTime.Now - coolDown[player.CSteamID]).TotalSeconds);
                    if ((DateTime.Now - coolDown[player.CSteamID]).TotalSeconds < PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds)
                    {
                        Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("error_cooldown"), Color.red);
                        return;
                    }
                    coolDown.Remove(player.CSteamID);
                }

                if (coolDown.ContainsKey(player.CSteamID))
                {
                    coolDown[player.CSteamID] = DateTime.Now;
                }
                else
                {
                    coolDown.Add(player.CSteamID, DateTime.Now);
                }

                if (requests.ContainsKey(rTo.CSteamID))
                {
                    requests[rTo.CSteamID] = player.CSteamID;
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_sent") + " " + rTo.CharacterName, Color.yellow);
                    Rocket.Unturned.Chat.UnturnedChat.Say(rTo, player.CharacterName + " " + PluginTPA.Instance.Translate("request_sent_1"), Color.yellow);
                }
                else
                {
                    requests.Add(rTo.CSteamID, player.CSteamID);
                    Rocket.Unturned.Chat.UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_sent") + " " + rTo.CharacterName, Color.yellow);
                    Rocket.Unturned.Chat.UnturnedChat.Say(rTo, player.CharacterName + " " + PluginTPA.Instance.Translate("request_sent_1"), Color.yellow);
                }
            }

        }
    }
}
