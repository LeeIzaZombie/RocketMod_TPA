using System;
using System.Drawing;
using Rocket.API.Commands;
using Rocket.API.Player;
using Rocket.Core.I18N;
using Rocket.Core.Player;
using Rocket.Core.User;

namespace RocketMod_TPA
{
    public class CommandTpaAccept : IChildCommand
    {
        private readonly PluginTpa _tpaPlugin;

        public CommandTpaAccept(PluginTpa tpaPlugin)
        {
            _tpaPlugin = tpaPlugin;
        }
        public string Name => "Accept";
        public string[] Aliases => new[] { "a", "yes" };
        public string Summary => "Accepts a teleport request";
        public string Description => null;
        public string Permission => "tpa.accept";
        public string Syntax => "";
        public IChildCommand[] ChildCommands => null;

        public bool SupportsUser(Type user)
        {
            return typeof(IPlayerUser).IsAssignableFrom(user);
        }

        public void Execute(ICommandContext context)
        {
            var player = ((IPlayerUser)context.User).GetPlayer();

            if (!_tpaPlugin.Requests.ContainsKey(player))
            {
                context.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_none", Color.Red);
                return;
            }

            IPlayer teleporter = _tpaPlugin.Requests[player];

            if (teleporter == null || !_tpaPlugin.CheckPlayer(teleporter))
            {
                _tpaPlugin.Requests.Remove(player);
                throw new PlayerNotOnlineException();
            }

            /*
                if (teleporter.Stance == EPlayerStance.DRIVING || teleporter.Stance == EPlayerStance.SITTING)
                {
                    UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("YouInCar"), Color.red);
                    UnturnedChat.Say(caller, PluginTPA.Instance.Translate("PlayerInCar"), Color.red);
                    Requests.Remove(player);
                    return;
                }
                */

            if (_tpaPlugin.ConfigurationInstance.TpaDelaySeconds > 0)
            {
                _tpaPlugin.DelayTeleport(player, teleporter);
                return;
            }

            /*
            if (_tpaPlugin.ConfigurationInstance.CancelOnBleeding && teleporter.Bleeding)
            {
                UnturnedChat.Say(teleporter, PluginTPA.Instance.Translate("error_bleeding"), Color.red);
                Requests.Remove(player);
                return;
            }
            */

            player.GetUser().SendLocalizedMessage(_tpaPlugin.Translations, "request_accepted", Color.Yellow, teleporter.Name);

            teleporter.GetUser().SendLocalizedMessage(_tpaPlugin.Translations, "request_accepted_1", Color.Yellow, player.Name);

            /*
            if (ConfigurationInstance.NinjaTP)
            {
                EffectManager.sendEffect((ushort)PluginTPA.Instance.Configuration.Instance.NinjaEffectID, 30, player.Position);
            }
            */

            _tpaPlugin.TeleportPlayer(teleporter, player);
            _tpaPlugin.Requests.Remove(player);
        }
    }
}