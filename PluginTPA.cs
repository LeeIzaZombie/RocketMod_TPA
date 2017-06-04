using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RocketMod_TPA
{
    public class PluginTPA : RocketPlugin<TPAConfiguration>
    {
        public static string version = "1.7.1.1";
        public static PluginTPA Instance;

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    { "help_line_1", "/tpa (playerName) - Sends a teleport request." },
                    { "help_line_2", "/tpa accept - Accepts your latest TPA request." },
                    { "help_line_3", "/tpa deny - Denys your latest TPA request." },
                    { "help_line_4", "/tpa abort - Abort your pending request." },
                    { "playerNotFound", "Could not find that player!" },
                    { "playerInCar", "Teleport failed, the player is in a car." },
                    { "YouInCar", "Teleport failed, you can't teleport in a car." },
                    { "TimeLeft", "Time left for another request: {0}" }, //Updated
                    { "Delay", "Your teleport will initiate in {0} {1}." }, //Updated
                    { "Seconds", "seconds" },
                    { "nopermission_send", "You do not have permission to send TPA requests." },
                    { "nopermission_accept", "You do not have permission to accept TPA requests." },
                    { "error_cooldown", "You may only send requests every {0} seconds." }, //Updated
                    { "error_bleeding", "You may not teleport when bleeding, resend TPA when you're safe." },
                    { "error_hurt", "You may not teleport when you're hurt!" },
                    { "error_player_left_server", "TPA has failed, the other player has left the server." }, //New
                    { "help_line_1", "TPA allows you to request a teleport to another player." },
                    { "request_success", "Your teleportation has been successful!" },
                    { "request_accepted", "You've accepted {0} TPA request!" }, //Updated
                    { "request_denied", "You've denied {0}'s request!" }, //Updated
                    { "request_accepted_1", "{0} accepted your tpa request!" }, //Updated
                    { "request_accepted_2", "{0} accepted your tpa request and will teleport in {1} {2}." }, //Updated
                    { "request_accepted_3", "You've accepted {0}'s tpa request and they will teleport in {1} {2}." }, //Updated
                    { "request_denied_1", "{0} denied your tpa request!" }, //Updated
                    { "request_sent", "You have sent a tpa request to {0}" }, //Updated
                    { "request_sent_1", "{0} sent you a tpa request, do /tpa accept to accept!" }, //Updated
                    { "request_sent_self", "TPA request failed, can't send to yourself." }, //New
                    { "request_pending", "You already have a request pending to this player." },
                    { "request_none", "You have no requests available!" },
                    { "request_abort", "You have aborted your TPA request." }, //New
                    { "request_abort_target", "{0} has aborted their request." }, //New
                    { "teleport_protection_enabled", "TPA teleport protection has been enabled for {0} seconds." },// two for TPA Protection.
                    { "teleport_protection_disabled", "TPA teleport protection has been disabled, you are now vulnerable." },
                    { "login_protection_enabled", "Login protection has been enabled on you for {0} seconds." },// two for login protections.
                    { "login_protection_disabled", "Login protection has been disabled, you are now vulnerable." },
                    { "translation_version_dont_edit", "3" },
                };
            }
        }

        protected override void Load()
        {
            PluginTPA.Instance = this;
            string str1 = Configuration.Instance.TPADelay.Format();
            string str2 = Configuration.Instance.TPACoolDown.Format();
            string str3 = Configuration.Instance.CancelOnBleeding.Format();
            string str4 = Configuration.Instance.CancelOnHurt.Format();
            string str6 = Configuration.Instance.NinjaTP.Format();
            string str7 = Configuration.Instance.TPATeleportProtection.Format();
            string str8 = Configuration.Instance.UseLoginProtection.Format();
            int num1 = Configuration.Instance.TPACoolDownSeconds;
            int num2 = Configuration.Instance.TPADelaySeconds;
            int num4 = Configuration.Instance.NinjaEffectID;
            int num5 = Configuration.Instance.TPATeleportProtectionSeconds;
            int num6 = Configuration.Instance.LoginProtectionTime;
            Logger.LogWarning("TPA by LeeIzaZombie, Version " + version);
            Logger.LogWarning("...");
            Logger.LogWarning("Current configuration:");
            Logger.LogWarning("...");
            Logger.LogWarning("Cooldown TPA requests: " + str2 + ", Seconds: " + num1);
            Logger.LogWarning("Delay teleporting: " + str1 + ", Seconds: " + num2);
            Logger.LogWarning("Cancel teleport if bleeding: " + str3);
            Logger.LogWarning("NinjaTP: " + str6 + ", EffectID: " + num4);
            Logger.LogWarning("TPA teleport protection: " + str7 + ", Protection time: " + num5);
            Logger.LogWarning("Login protection: " + str8 + ", Protection time: " + num6);
            Logger.LogWarning("[Delay] Cancel teleport if hurt: " + str4);
            Logger.LogWarning("...");
            Logger.LogWarning("Checking for problems:");
            int i = 0;
            if (Translate("translation_version_dont_edit") != "3")
            {
                Logger.LogError("Your translations file is out of date, please reload the plugin after deleting old configuration folder.");
                i++;
            }
            if (num1 < 0)
            {
                Logger.LogError("Cooldown Seconds configuration is invalid, please fix it.");
                i++;
            }
            if (num2 < 0)
            {
                Logger.LogError("Delay Seconds configuration is invalid, please fix it.");
                i++;
            }
            if (num5 < 0)
            {
                Logger.LogError("Protection time configuration is invalid, please fix it.");
                i++;
            }
            if (num6 < 0)
            {
                Logger.LogError("Login protection time is invalid, please fix it.");
                i++;
            }
            if (i != 0)
            {
                Logger.LogWarning("Error checking done, " + i + " configuration errors found!");
            }
            else
            {
                Logger.LogWarning("No errors have been found!");
            }
            Logger.LogWarning("...");
            Logger.LogWarning("Always welcome to suggestions! If you find a bug, please report it!");

            U.Events.OnPlayerConnected += TPA_PlayerJoin;
            U.Events.OnPlayerDisconnected += TPA_PlayerLeave;
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= TPA_PlayerJoin;
            U.Events.OnPlayerDisconnected -= TPA_PlayerLeave;
        }

        private void TPA_PlayerLeave(UnturnedPlayer player)
        {
            if (player != null)
            {
                if (CommandTPA.requests.ContainsKey(player.CSteamID))
                {
                    lock (CommandTPA.requests)
                        CommandTPA.requests.Remove(player.CSteamID);
                }
                player.GetComponent<TPAProtectionComponent>().EventCleanup();
            }
        }

        private void TPA_PlayerJoin(UnturnedPlayer player)
        {
            if (CommandTPA.requests.ContainsKey(player.CSteamID))
            {
                lock (CommandTPA.requests)
                    CommandTPA.requests.Remove(player.CSteamID);
            }
            if (Configuration.Instance.UseLoginProtection)
            {
                TPAProtectionComponent pc = player.GetComponent<TPAProtectionComponent>();
                pc.LoginProtection = true;
                pc.LoginProtectionStart = DateTime.Now;
                pc.Protected = true;
                UnturnedChat.Say(player, Translate("login_protection_enabled", Configuration.Instance.LoginProtectionTime), UnityEngine.Color.yellow);
            }
        }

        // Runs through the teleport queue.
        public void Update()
        {
            if (CommandTPA.teleportQueue.Count > 0)
            {
                KeyValuePair<UnturnedPlayer, UnturnedPlayer> value;
                lock (CommandTPA.teleportQueue)
                {
                    value = CommandTPA.teleportQueue.Dequeue();
                }
                if ( value.Key == null || value.Value == null)
                    return;
                value.Key.Teleport(value.Value);
                Logger.Log(string.Format("Player: {0} [{1}] ({2}), has TPA'd to player: {3} [{4}] ({5}), at location: {6}.", value.Key.CharacterName, value.Key.SteamName, value.Key.CSteamID, value.Value.CharacterName, value.Value.SteamName, value.Value.CSteamID, value.Value.Player.transform.position));
            }
        }
    }

    public static class Extensions
    {
        // Format a boolean to enabled or disabled.
        public static string Format(this bool value)
        {
            return value ? "enabled" : "disabled";
        }
    }
}

