using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using System.Collections.Generic;
using System.Linq;

namespace RocketMod_TPA
{
    public class PluginTPA : RocketPlugin<TPAConfiguration>
    {
        public static string version = "1.7.0.2";
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
                    { "request_pending", "You already have a request pending to this player." },
                    { "request_none", "You have no requests available!" },
                    { "request_abort", "You have aborted your TPA request." }, //New
                    { "translation_version_dont_edit", "3" }
                };
            }
        }

        protected override void Load()
        {
            PluginTPA.Instance = this;
            string str1 = !Configuration.Instance.TPADelay ? "disabled" : "enabled";
            string str2 = !Configuration.Instance.TPACoolDown ? "disabled" : "enabled";
            string str3 = !Configuration.Instance.CancelOnBleeding ? "disabled" : "enabled";
            string str4 = !Configuration.Instance.CancelOnHurt ? "disabled" : "enabled";
            string str6 = !Configuration.Instance.NinjaTP ? "disabled" : "enabled";
            //string str5 = !Configuration.Instance.TPADoubleTap ? "disabled" : "enabled";
            int num1 = Configuration.Instance.TPACoolDownSeconds;
            int num2 = Configuration.Instance.TPADelaySeconds;
            int num4 = Configuration.Instance.NinjaEffectID;
            //int num3 = Configuration.Instance.DoubleTapDelaySeconds;
            Logger.LogWarning("TPA by LeeIzaZombie, Version " + PluginTPA.version);
            Logger.LogWarning("...");
            Logger.LogWarning("Current configuration:");
            Logger.LogWarning("...");
            Logger.LogWarning("Cooldown TPA requests: " + str2 + ", Seconds: " + num1);
            Logger.LogWarning("Delay teleporting: " + str1 + ", Seconds: " + num2);
            Logger.LogWarning("Cancel teleport if bleeding: " + str3);
            //Logger.LogWarning("Double Tap: " + str5 + ", Seconds: " + num3);
            Logger.LogWarning("NinjaTP: " + str6 + ", EffectID: " + num4);
            Logger.LogWarning("[Delay] Cancel teleport if hurt: " + str4);
            Logger.LogWarning("...");
            Logger.LogWarning("Checking for problems:");
            int i = 0;
            if (Translate("translation_version_dont_edit") != "3")
            {
                Logger.LogError("Your translations file is out of date, please reload the plugin after deleting old configuration folder.");
                i++;
            }
            if (Configuration.Instance.TPACoolDownSeconds < 0)
            {
                Logger.LogError("Cooldown Seconds configuration is invalid, please fix it.");
                i++;
            }
            if (Configuration.Instance.TPADelaySeconds < 0)
            {
                Logger.LogError("Delay Seconds configuration is invalid, please fix it.");
                i++;
            }
            if (i != 0)
            {
                Logger.LogWarning("Error checking done.");
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

        private void TPA_PlayerLeave(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            if (CommandTPA.requests.ContainsKey(player.CSteamID))
            {
                CommandTPA.requests.Remove(player.CSteamID);
            }
        }

        private void TPA_PlayerJoin(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            if (CommandTPA.requests.ContainsKey(player.CSteamID))
            {
                CommandTPA.requests.Remove(player.CSteamID);
            }
        }
    }
}

