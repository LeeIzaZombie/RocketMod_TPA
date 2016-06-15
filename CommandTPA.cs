using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RocketMod_TPA
{
    public class CommandTPA : IRocketCommand
    {
        #region Declarations
        public static Dictionary<CSteamID, CSteamID> requests = new Dictionary<CSteamID, CSteamID>();
        private Dictionary<CSteamID, DateTime> coolDown = new Dictionary<CSteamID, DateTime>();
        private Dictionary<CSteamID, byte> health = new Dictionary<CSteamID, byte>();

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

        public bool RunFromConsole
        {
            get
            {
                return false;
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

            #region Accept

            if (command[0].ToString().ToLower() == "accept" || command[0].ToString().ToLower() == "a" || command[0].ToString().ToLower() == "yes")
            {
                if (!player.HasPermission("tpa.accept") && !player.IsAdmin)
                {
                    UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_accept"), Color.red);
                    return;
                }

                if (requests.ContainsKey(player.CSteamID))
                {
                    UnturnedPlayer teleporter = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);

                    if (teleporter == null || !CheckPlayer(requests[player.CSteamID]))
                    {
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("playerNotFound"), Color.red);
                        requests.Remove(player.CSteamID);
                        return;
                    }

                    if (teleporter.Stance == EPlayerStance.DRIVING || teleporter.Stance == EPlayerStance.SITTING)
                    {
                        UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("YouInCar"), Color.red);
                        UnturnedChat.Say(caller, PluginTPA.Instance.Translate("PlayerInCar"), Color.red);
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
                    return;
                }
                else
                {
                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                    return;
                }
            }

            #endregion

            #region Deny

            if (command[0].ToString() == "deny" || command[0].ToString().ToLower() == "d" || command[0].ToString().ToLower() == "no")
            {
                if (requests.ContainsKey(player.CSteamID))
                {
                    UnturnedPlayer teleporter = UnturnedPlayer.FromCSteamID(requests[player.CSteamID]);
                    requests.Remove(player.CSteamID);
                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_denied", teleporter.CharacterName), Color.yellow);
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_denied_1", player.CharacterName), Color.red);
                    return;
                }
                else
                {
                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red); return;
                }
            }
            #endregion

            #region Abort

            if (command[0].ToString() == "abort")
            {
                bool flag = false;
                foreach (Steamworks.CSteamID id in CommandTPA.requests.Keys)
                {
                    if (CommandTPA.requests[id] == player.CSteamID)
                    {
                        CommandTPA.requests.Remove(id);
                        UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_abort"), Color.yellow);
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("request_none"), Color.red);
                }
                return;
            }
            #endregion

            #region Send

            if (!player.HasPermission("tpa.send"))
            {
                UnturnedChat.Say(player, PluginTPA.Instance.Translate("nopermission_send"), Color.red);
                return;
            }

            UnturnedPlayer requestTo = UnturnedPlayer.FromName(command[0].ToString());

            if (requestTo == null)
            {
                UnturnedChat.Say(caller, PluginTPA.Instance.Translate("playerNotFound"), Color.red);
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

            if (requests.ContainsKey(requestTo.CSteamID))
                requests[requestTo.CSteamID] = player.CSteamID;
            else
                requests.Add(requestTo.CSteamID, player.CSteamID);

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
            #endregion

        }
        
        private void DelayTP(UnturnedPlayer player, UnturnedPlayer teleporter, int delay)
        {
            new Thread((ThreadStart)(() =>
            {
                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_accepted_2", player.CharacterName, delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);
                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("Delay", delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);
                UnturnedChat.Say(player, PluginTPA.Instance.Translate("request_accepted_3", teleporter.CharacterName, delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);

                if (this.health.ContainsKey(teleporter.CSteamID))
                    this.health[teleporter.CSteamID] = teleporter.Health;
                else
                    this.health.Add(teleporter.CSteamID, teleporter.Health);

                Thread.Sleep(delay * 1000);

                if (PluginTPA.Instance.Configuration.Instance.CancelOnBleeding && teleporter.Bleeding)
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_bleeding"), Color.red);
                    requests.Remove(player.CSteamID);
                }
                else if (PluginTPA.Instance.Configuration.Instance.CancelOnHurt && health.ContainsKey(teleporter.CSteamID) && health[teleporter.CSteamID] > teleporter.Health)
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_hurt"), Color.red);
                    requests.Remove(player.CSteamID);
                    this.health.Remove(teleporter.CSteamID);
                }
                else
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("request_success"), Color.yellow);
                    //teleporter.Teleport(player);
                    TPplayer(teleporter, player);
                    requests.Remove(player.CSteamID);
                    this.health.Remove(teleporter.CSteamID);
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
                EffectManager.sendEffect((ushort)PluginTPA.Instance.Configuration.Instance.NinjaEffectID, 30, player.Position);
            }
            player.Teleport(target);
            //if (PluginTPA.Instance.Configuration.Instance.TPADoubleTap)
            //{
            //    Thread.Sleep(PluginTPA.Instance.Configuration.Instance.DoubleTapDelaySeconds * 1000);
            //    player.Teleport(target);
            //}
        }

        private bool CheckPlayer(Steamworks.CSteamID plr)
        {
            bool flag = false;
            foreach (SteamPlayer sp in Provider.Players)
            {
                if (sp.SteamPlayerID.CSteamID == plr)
                {
                    flag = true;
                }
            }

            return flag;
        }
    }
}