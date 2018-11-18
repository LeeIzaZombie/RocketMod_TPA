using System;
using System.Drawing;
using Rocket.API.Commands;
using Rocket.API.Player;
using Rocket.Core.I18N;
using Rocket.Core.Player;
using Rocket.Core.User;

namespace RocketMod_TPA
{
    public class CommandTpaAbort : IChildCommand
    {
        private readonly PluginTpa _tpaPlugin;

        public CommandTpaAbort(PluginTpa tpaPlugin)
        {
            _tpaPlugin = tpaPlugin;
        }

        public string Name => "Abort";
        public string[] Aliases => new[] { "cancel" };
        public string Summary => "Aborts a teleport request.";
        public string Description => null;
        public string Permission => "tpa.abort";
        public string Syntax => "";
        public IChildCommand[] ChildCommands => null;

        public bool SupportsUser(Type user)
        {
            return typeof(IPlayerUser).IsAssignableFrom(user);
        }

        public void Execute(ICommandContext context)
        {
            var player = ((IPlayerUser)context.User).GetPlayer();
            bool removed = _tpaPlugin.Requests.Remove(player);

            if (removed)
                context.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_abort", Color.Yellow);
            else
                context.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_none", Color.Red);
        }
    }
}