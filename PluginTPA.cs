using System;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Rocket.API.DependencyInjection;
using Rocket.API.Entities;
using Rocket.API.Player;
using Rocket.Core.I18N;
using Rocket.Core.Player;

namespace RocketMod_TPA
{
    public class PluginTpa : Plugin<TpaConfiguration>
    {
        public PluginTpa(IDependencyContainer container) : base("TPA", container)
        {
        }

        public static string Version = "1.8.0.0";

        public override Dictionary<string, string> DefaultTranslations => new Dictionary<string, string>
        {
            { "delay", "Your teleport will initiate in {0} seconds." }, //Updated
            { "nopermission_send", "You do not have permission to send TPA requests." },
            { "nopermission_accept", "You do not have permission to accept TPA requests." },
            { "error_cooldown", "You may only send requests every {0} seconds." }, //Updated
            { "error_hurt", "You may not teleport when you're hurt!" },
            { "request_success", "Your teleportation has been successful!" },
            { "request_accepted", "You've accepted {0} TPA request!" }, //Updated
            { "request_denied", "You've denied {0}'s request!" }, //Updated
            { "request_accepted_1", "{0} accepted your tpa request!" }, //Updated
            { "request_accepted_2", "You've accepted {0}'s tpa request and they will teleport in {1} seconds." }, //Updated
            { "request_denied_1", "{0} denied your tpa request!" }, //Updated
            { "request_sent", "You have sent a tpa request to {0}" }, //Updated
            { "request_sent_1", "{0} sent you a tpa request, do /tpa accept to accept!" }, //Updated
            { "request_pending", "You already have a request pending to this player." },
            { "request_none", "You have no requests available!" },
            { "request_abort", "You have aborted your TPA request." }, //New
        };

        public readonly Dictionary<IPlayer, IPlayer> Requests = new Dictionary<IPlayer, IPlayer>();
        public readonly Dictionary<IPlayer, DateTime> CoolDowns = new Dictionary<IPlayer, DateTime>();
        public readonly Dictionary<IPlayer, double> Healths = new Dictionary<IPlayer, double>();

        public void DelayTeleport(IPlayer player, IPlayer teleporter)
        {
            uint delay = ConfigurationInstance.TpaDelaySeconds;
            new Thread(() =>
            {
                teleporter.GetUser().SendLocalizedMessage(Translations, "request_accepted_1", Color.Yellow, player.Name);
                teleporter.GetUser().SendLocalizedMessage(Translations, "delay", Color.Yellow, delay);

                player.GetUser().SendLocalizedMessage(Translations, "request_accepted_2", Color.Yellow, teleporter.Name, delay);

                var entity = teleporter.GetEntity() as ILivingEntity;
                if (entity != null)
                {
                    if (Healths.ContainsKey(teleporter))
                        Healths[teleporter] = entity.Health;
                    else
                        Healths.Add(teleporter, entity.Health);
                }

                Thread.Sleep((int) delay * 1000);
                /*
                if (ConfigurationInstance.CancelOnBleeding && teleporter.Bleeding)
                {
                    UnturnedChat.Say(teleporter, PluginTpa.Instance.Translate("error_bleeding"), Color.red);
                    Requests.Remove(player);
                }
                else */
                if (ConfigurationInstance.CancelTpaOnHurt && Healths.ContainsKey(teleporter) && entity != null && Healths[teleporter] > entity.Health)
                {
                    teleporter.GetUser().SendLocalizedMessage(Translations, "error_hurt", Color.Red);
                    Requests.Remove(player);
                    Healths.Remove(teleporter);
                }
                else
                {
                    teleporter.GetUser().SendLocalizedMessage(Translations, "request_success", Color.Yellow);
                    TeleportPlayer(teleporter, player);
                    Requests.Remove(player);
                    Healths.Remove(teleporter);
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        public void TeleportPlayer(IPlayer teleporter, IPlayer player)
        {
            teleporter.GetEntity().Teleport(player.GetEntity().Position);
        }

        public bool CheckPlayer(IPlayer player)
        {
            return player.IsOnline;
        }
    }
}

