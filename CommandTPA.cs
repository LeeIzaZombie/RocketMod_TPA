using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RocketMod_TPA
{
    public class CommandTPA : IRocketCommand
    {
        #region Declarations
        public List<string> Permissions
        {
            get
            {
                return new List<string>()
                {
                    "CommandTPA.tpa"
                };
            }
        }

        public AllowedCaller AllowedCaller
        {
            get
            {
                return AllowedCaller.Player;
            }
        }

        public string Name
        {
            get
            {
                return "tpa";
            }
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
            get
            {
                return "Request a teleport to a player, accept or deny other requests.";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>();
            }
        }

        #endregion

        public void Execute(IRocketPlayer caller, string[] command)
        {

            #region Help
            UnturnedPlayer player = (UnturnedPlayer)caller;
            TPAProtectionComponent cComponent = player.GetComponent<TPAProtectionComponent>();
            TPAProtectionComponent tComponent = null;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_1"), Color.yellow);
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_2"), Color.yellow);
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_3"), Color.yellow);
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_4"), Color.yellow);
                return;
            }
            #endregion
            CSteamID lastTargetID = CSteamID.Nil;
            string commandValue = string.Join(" ", command);
            switch (commandValue.ToLower())
            {
                #region Accept
                // Accept a TPA.
                case "accept":
                case "a":
                case "yes":
                    if (!player.HasPermission("tpa.accept") && !player.IsAdmin)
                    {
                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_accept"), Color.red);
                        return;
                    }

                    if (cComponent.TPARequestList.Count > 0)
                    {
                        foreach (CSteamID cSteamID in cComponent.TPARequestList)
                        {
                            UnturnedPlayer teleporter = UnturnedPlayer.FromCSteamID(cSteamID);
                            if (teleporter.Player == null || teleporter.CSteamID.IsInvalid())
                            {
                                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("error_player_left_server"), Color.red);
                                continue;
                            }

                            if (teleporter.Stance == EPlayerStance.DRIVING || teleporter.Stance == EPlayerStance.SITTING)
                            {
                                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("YouInCar"), Color.red);
                                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("PlayerInCar"), Color.red);
                                continue;
                            }
                            tComponent = teleporter.GetComponent<TPAProtectionComponent>();
                            if (PluginTPA.Instance.Configuration.Instance.TPADelay)
                            {
                                tComponent.DelayTP(player);
                                continue;
                            }

                            if (PluginTPA.Instance.Configuration.Instance.CancelOnBleeding && teleporter.Bleeding)
                            {
                                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_bleeding"), Color.red);
                                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("error_bleeding1", teleporter.CharacterName), Color.red);
                                continue;
                            }

                            UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_accepted", teleporter.CharacterName), Color.yellow);
                            UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_accepted_1", player.CharacterName), Color.yellow);
                            tComponent.TPplayer(player);
                            if (PluginTPA.Instance.Configuration.Instance.TPATeleportProtection)
                            {
                                tComponent.RunTeleportProtections();
                            }
                        }
                        cComponent.TPARequestList.Clear();
                        return;
                    }
                    else
                    {
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                        return;
                    }
                #endregion

                #region Deny
                // Deny a TPA
                case "deny":
                case "d":
                case "no":
                    if (cComponent.TPARequestList.Count > 0)
                    {
                        foreach (CSteamID cSteamID in cComponent.TPARequestList)
                        {
                            UnturnedPlayer teleporter = UnturnedPlayer.FromCSteamID(cSteamID);
                            UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_denied", teleporter.Player == null ? "?" : teleporter.CharacterName), Color.yellow);
                            if (teleporter.Player != null)
                                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_denied_1", player.CharacterName), Color.red);
                        }
                        cComponent.TPARequestList.Clear();
                    }
                    else
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                    return;
                #endregion

                #region Abort
                // Abort a TPA.
                case "abort":
                    lastTargetID = CSteamID.Nil;
                    SteamPlayer splayer = Provider.clients.FirstOrDefault(csID => UnturnedPlayer.FromSteamPlayer(csID).GetComponent<TPAProtectionComponent>().TPARequestList.Contains(player.CSteamID));
                    if (splayer != null)
                        lastTargetID = splayer.playerID.steamID;
                    if (lastTargetID != CSteamID.Nil)
                    {
                        UnturnedPlayer.FromCSteamID(lastTargetID).GetComponent<TPAProtectionComponent>().TPARequestList.Remove(player.CSteamID);
                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_abort"), Color.yellow);
                        UnturnedChat.Say(lastTargetID, PluginTPA.Instance.Translate("request_abort_target", player.CharacterName), Color.yellow);
                        return;
                    }
                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                    return;
                #endregion

                #region Send
                // Send a TPA.
                default:
                    if (!player.HasPermission("tpa.send"))
                    {
                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_send"), Color.red);
                        return;
                    }

                    UnturnedPlayer requestTo = UnturnedPlayer.FromName(commandValue);
                    if (requestTo == null)
                    {
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("playerNotFound"), Color.red);
                        return;
                    }
                    if (requestTo.CSteamID == player.CSteamID)
                    {
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_sent_self"), Color.red);
                        return;
                    }
                    if (PluginTPA.Instance.Configuration.Instance.TPACoolDown)
                    {
                        double timeLeft = (DateTime.Now - cComponent.lastCooldownStart).TotalSeconds;
                        if (timeLeft < PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds)
                        {
                            UnturnedChat.Say(caller, PluginTPA.Instance.Translate("error_cooldown", PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds), Color.red);
                            UnturnedChat.Say(caller, PluginTPA.Instance.Translate("TimeLeft", System.Math.Round((PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds - timeLeft), 2), Color.yellow));
                            return;
                        }
                    }

                    tComponent = requestTo.GetComponent<TPAProtectionComponent>();
                    lastTargetID = CSteamID.Nil;
                    splayer = Provider.clients.FirstOrDefault(csID => UnturnedPlayer.FromSteamPlayer(csID).GetComponent<TPAProtectionComponent>().TPARequestList.Contains(player.CSteamID));
                    if (splayer != null)
                        lastTargetID = splayer.playerID.steamID;
                    if (lastTargetID != CSteamID.Nil && requestTo.CSteamID != lastTargetID)
                    {
                        // remove player from the last sent target player list, if request is sent to another player.
                        UnturnedPlayer.FromCSteamID(lastTargetID).GetComponent<TPAProtectionComponent>().TPARequestList.Remove(player.CSteamID);
                    }

                    if (tComponent.TPARequestList.Contains(player.CSteamID))
                    {
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_pending"), Color.red);
                        return;
                    }
                    else
                        tComponent.TPARequestList.Add(player.CSteamID);

                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_sent", requestTo.CharacterName), Color.yellow);
                    UnturnedChat.Say(requestTo, PluginTPA.Instance.Translate("request_sent_1", player.CharacterName), Color.yellow);

                    cComponent.lastCooldownStart = DateTime.Now;
                    break;
                #endregion
            }
        }
    }
}