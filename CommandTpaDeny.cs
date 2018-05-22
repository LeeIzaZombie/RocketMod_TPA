using System;
using System.Drawing;
using Rocket.API.Commands;
using Rocket.API.Player;
using Rocket.Core.I18N;
using Rocket.Core.Player;
using Rocket.Core.User;

namespace RocketMod_TPA
{
    public class CommandTpaDeny : IChildCommand
    {
        private readonly PluginTpa _tpaPlugin;

        public CommandTpaDeny(PluginTpa tpaPlugin)
        {
            _tpaPlugin = tpaPlugin;
        }
        public string Name => "Deny";
        public string[] Aliases => new[] { "d", "no" };
        public string Summary => "Denies the TPA request.";
        public string Description => null;
        public string Permission => "tpa.deny";
        public string Syntax => "";
        public IChildCommand[] ChildCommands => null;

        public bool SupportsUser(Type user)
        {
            return typeof(IPlayerUser).IsAssignableFrom(user);
        }

        public void Execute(ICommandContext context)
        {
            var player = ((IPlayerUser)context.User).GetPlayer().Extend();

            if (_tpaPlugin.Requests.ContainsKey(player))
            {
                IPlayer teleporter = _tpaPlugin.Requests[player];
                _tpaPlugin.Requests.Remove(player);

                player.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_denied", Color.Yellow, teleporter.Name);
                teleporter.GetUser().SendLocalizedMessage(_tpaPlugin.Translations, "request_denied_1", Color.Red, player.Name);
                return;
            }

            player.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_none", Color.Red);
        }
    }
}