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
        internal static Dictionary<CSteamID, CSteamID> requests = new Dictionary<CSteamID, CSteamID>();
        // Queue to sync delayed teleports to main thread.
        internal static Queue<KeyValuePair<UnturnedPlayer, UnturnedPlayer>> teleportQueue = new Queue<KeyValuePair<UnturnedPlayer, UnturnedPlayer>>();
        private Dictionary<CSteamID, DateTime> coolDown = new Dictionary<CSteamID, DateTime>();
        private Dictionary<CSteamID, byte> health = new Dictionary<CSteamID, byte>();

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
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_1"), Color.yellow);
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_2"), Color.yellow);
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_3"), Color.yellow);
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("help_line_4"), Color.yellow);
                return;
            }
            #endregion
            CSteamID targetID = CSteamID.Nil;
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

                    if (requests.ContainsKey(player.CSteamID))
                    {
                        UnturnedPlayer teleporter = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);

                        if (teleporter.Player == null || IsInvalid(requests[player.CSteamID]))
                        {
                            UnturnedChat.Say(caller, PluginTPA.Instance.Translate("error_player_left_server"), Color.red);
                            lock (requests)
                                requests.Remove(player.CSteamID);
                            return;
                        }

                        if (teleporter.Stance == EPlayerStance.DRIVING || teleporter.Stance == EPlayerStance.SITTING)
                        {
                            UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("YouInCar"), Color.red);
                            UnturnedChat.Say(caller, PluginTPA.Instance.Translate("PlayerInCar"), Color.red);
                            lock (requests)
                                requests.Remove(player.CSteamID);
                            return;
                        }

                        if (PluginTPA.Instance.Configuration.Instance.TPADelay)
                        {
                            DelayTP(player, teleporter, PluginTPA.Instance.Configuration.Instance.TPADelaySeconds);
                            return;
                        }

                        if (PluginTPA.Instance.Configuration.Instance.CancelOnBleeding && teleporter.Bleeding)
                        {
                            UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_bleeding"), Color.red);
                            requests.Remove(player.CSteamID);
                            return;
                        }

                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_accepted", teleporter.CharacterName), Color.yellow);
                        UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_accepted_1", player.CharacterName), Color.yellow);
                        //teleporter.Teleport(player);
                        TPplayer(teleporter, player);
                        requests.Remove(player.CSteamID);
                        if (PluginTPA.Instance.Configuration.Instance.TPATeleportProtection)
                        {
                            new Thread(() =>
                            {
                                TPProtect(teleporter, PluginTPA.Instance.Configuration.Instance.TPATeleportProtectionSeconds);
                            })
                            {
                                IsBackground = true
                            }.Start();
                        }
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
                    if (requests.ContainsKey(player.CSteamID))
                    {
                        UnturnedPlayer teleporter = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);
                        lock (requests)
                            requests.Remove(player.CSteamID);
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_denied", teleporter.Player == null ? "?" : teleporter.CharacterName), Color.yellow);
                        if (teleporter.Player != null)
                            UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_denied_1", player.CharacterName), Color.red);
                        return;
                    }
                    else
                    {
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                        return;
                    }
                #endregion

                #region Abort
                // Abort a TPA.
                case "abort":
                    targetID = requests.FirstOrDefault(tID => tID.Value == player.CSteamID).Key;
                    if (targetID != CSteamID.Nil)
                    {
                        lock (requests)
                            requests.Remove(targetID);
                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_abort"), Color.yellow);
                        UnturnedChat.Say(targetID, PluginTPA.Instance.Translate("request_abort_target", player.CharacterName), Color.yellow);
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
                        if (coolDown.ContainsKey(player.CSteamID))
                        {
                            int timeLeft = Convert.ToInt32(System.Math.Abs((DateTime.Now - coolDown[player.CSteamID]).TotalSeconds));
                            if (timeLeft < PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds)
                            {
                                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("error_cooldown", PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds), Color.red);
                                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("TimeLeft", (PluginTPA.Instance.Configuration.Instance.TPACoolDownSeconds - timeLeft), Color.yellow));
                                return;
                            }
                            coolDown.Remove(player.CSteamID);
                        }
                    }

                    if (requests.ContainsKey(requestTo.CSteamID))
                    {
                        if (requests[requestTo.CSteamID] == player.CSteamID)
                        {
                            UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_pending"), Color.red);
                            return;
                        }
                    }

                    targetID = requests.FirstOrDefault(id => id.Value == player.CSteamID).Key;

                    lock (requests)
                    {
                        // Switch a request to a different player if sent to a different player.
                        if (targetID != CSteamID.Nil && targetID != requestTo.CSteamID)
                        {
                            requests.Remove(targetID);
                            UnturnedChat.Say(targetID, PluginTPA.Instance.Translate("request_abort_target", player.CharacterName), Color.yellow);
                        }
                        if (requests.ContainsKey(requestTo.CSteamID))
                            requests[requestTo.CSteamID] = player.CSteamID;
                        else
                            requests.Add(requestTo.CSteamID, player.CSteamID);
                    }

                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_sent", requestTo.CharacterName), Color.yellow);
                    UnturnedChat.Say(requestTo, PluginTPA.Instance.Translate("request_sent_1", player.CharacterName), Color.yellow);


                    if (coolDown.ContainsKey(player.CSteamID))
                    {
                        coolDown[player.CSteamID] = DateTime.Now;
                    }
                    else
                    {
                        coolDown.Add(player.CSteamID, DateTime.Now);
                    }
                    break;
                #endregion
            }
        }
        
        private void DelayTP(UnturnedPlayer player, UnturnedPlayer teleporter, int delay)
        {
            new Thread((ThreadStart)(() =>
            {
                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_accepted_2", player.CharacterName, delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);
                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("Delay", delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);
                UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_accepted_3", teleporter.CharacterName, delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);

                CSteamID playerID = player.CSteamID;
                CSteamID teleporterID = teleporter.CSteamID;

                lock (requests)
                {
                    if (this.health.ContainsKey(teleporterID))
                        this.health[teleporterID] = teleporter.Health;
                    else
                        this.health.Add(teleporterID, teleporter.Health);
                }

                Thread.Sleep(delay * 1000);
                // Check to see if the players are still on the server. Run cleanup and return if they aren't.
                if (player == null || teleporter == null || IsInvalid(playerID) || IsInvalid(teleporterID))
                {
                    lock (requests)
                    {
                        if (requests.ContainsKey(playerID))
                            requests.Remove(playerID);
                        health.Remove(teleporterID);
                    }
                    if (teleporter != null)
                        UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_player_left_server"), Color.red);
                    if (player != null)
                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("error_player_left_server"), Color.red);
                    return;
                }

                if (PluginTPA.Instance.Configuration.Instance.CancelOnBleeding && teleporter.Bleeding)
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_bleeding"), Color.red);
                    lock (requests)
                    {
                        requests.Remove(player.CSteamID);
                        health.Remove(teleporter.CSteamID);
                    }
                }
                else if (PluginTPA.Instance.Configuration.Instance.CancelOnHurt && health.ContainsKey(teleporter.CSteamID) && health[teleporter.CSteamID] > teleporter.Health)
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_hurt"), Color.red);
                    lock (requests)
                    {
                        requests.Remove(player.CSteamID);
                        health.Remove(teleporter.CSteamID);
                    }
                }
                else
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_success"), Color.yellow);
                    //teleporter.Teleport(player);
                    TPplayer(teleporter, player);
                    lock (requests)
                    {
                        requests.Remove(player.CSteamID);
                        health.Remove(teleporter.CSteamID);
                    }
                    if (PluginTPA.Instance.Configuration.Instance.TPATeleportProtection)
                        TPProtect(teleporter, PluginTPA.Instance.Configuration.Instance.TPATeleportProtectionSeconds);
                }
            }))
            {
                IsBackground = true
            }.Start();
        }

        private void TPplayer(UnturnedPlayer player, UnturnedPlayer target)
        {
            if (PluginTPA.Instance.Configuration.Instance.NinjaTP)
            {
                EffectManager.sendEffect(PluginTPA.Instance.Configuration.Instance.NinjaEffectID, 30, player.Position);
            }
            lock (teleportQueue)
            {
                teleportQueue.Enqueue(new KeyValuePair<UnturnedPlayer, UnturnedPlayer>(player, target));
            }
            //player.Teleport(target);
            //if (PluginTPA.Instance.Configuration.Instance.TPADoubleTap)
            //{
            //    Thread.Sleep(PluginTPA.Instance.Configuration.Instance.DoubleTapDelaySeconds * 1000);
            //    player.Teleport(target);
            //}
        }

        private bool IsInvalid(CSteamID plrID)
        {
            return Provider.clients.FirstOrDefault(id => id.playerID.steamID == plrID) == null;
        }

        private void TPProtect(UnturnedPlayer target, int protectTime)
        {
            UnturnedPlayerFeatures features = target.GetComponent<UnturnedPlayerFeatures>();
            TPAProtectionComponent protections = target.GetComponent<TPAProtectionComponent>();
            // Don't execute if the player already has god mode enabled.
            if (!features.GodMode && (!protections.Protected || (protections.Protected && protections.LoginProtection)))
            {
                if (!protections.LoginProtection)
                    protections.Protected = true;
                else
                    protections.LoginProtection = false;
                UnturnedChat.Say(target, PluginTPA.Instance.Translate("teleport_protection_enabled", protectTime), Color.yellow);
                CSteamID tID = target.CSteamID;
                Thread.Sleep(protectTime * 1000);
                // Check to see if the players are still on the server, return if they aren't.
                if (target == null || IsInvalid(tID))
                    return;
                UnturnedChat.Say(target, PluginTPA.Instance.Translate("teleport_protection_disabled"), Color.yellow);
                protections.Protected = false;
            }
        }
    }
}