using System;
using System.Drawing;
using System.Linq;
using Rocket.API.Commands;
using Rocket.API.Permissions;
using Rocket.API.Player;
using Rocket.API.Plugins;
using Rocket.Core.Commands;
using Rocket.Core.I18N;
using Rocket.Core.Permissions;
using Rocket.Core.Player;
using Rocket.Core.User;

namespace RocketMod_TPA
{
    public class CommandTpa : ICommand
    {
        private readonly PluginTpa _tpaPlugin;

        public CommandTpa(IPlugin plugin)
        {
            _tpaPlugin = (PluginTpa)plugin;
        }

        public string Name => "tpa";
        public string[] Aliases => null;

        public string Permission => null;
        public string Syntax => "tpa [player]";

        public IChildCommand[] ChildCommands => new IChildCommand[]
        {
            new CommandTpaAbort(_tpaPlugin),
            new CommandTpaAccept(_tpaPlugin),
            new CommandTpaDeny(_tpaPlugin)
        };

        public string Summary => "Requests a teleport to a player.";
        public string Description => null;


        public bool SupportsUser(Type user)
        {
            return typeof(IPlayerUser).IsAssignableFrom(user);
        }

        public void Execute(ICommandContext context)
        {
            var parameters = context.Parameters;
            var permissionProvider = context.Container.Resolve<IPermissionProvider>();

            if (parameters.Length != 1)
            {
                throw new CommandWrongUsageException();
            }

            var player = ((IPlayerUser)context.User).GetPlayer();
            if (permissionProvider.CheckPermission(player, "tpa.send") != PermissionResult.Grant)
            {
                throw new NotEnoughPermissionsException(context.User, "tpa.send");
            }

            IPlayer requestTo = parameters.Get<IPlayer>(0);

            if (requestTo == null || !requestTo.IsOnline)
            {

                string name = parameters.Get<string>(0);
                throw new PlayerNameNotFoundException(name);
            }

            if (_tpaPlugin.ConfigurationInstance.TpaCoolDown)
            {
                if (_tpaPlugin.CoolDowns.ContainsKey(player))
                {
                    int timeLeft = Convert.ToInt32(System.Math.Abs((DateTime.Now - _tpaPlugin.CoolDowns[player]).TotalSeconds));
                    if (timeLeft < _tpaPlugin.ConfigurationInstance.TpaCoolDownSeconds)
                    {
                        context.User.SendLocalizedMessage(_tpaPlugin.Translations, "error_cooldown", Color.Red);
                        context.User.SendLocalizedMessage(_tpaPlugin.Translations, "time_left", Color.Yellow, _tpaPlugin.ConfigurationInstance.TpaCoolDownSeconds - timeLeft);
                        return;
                    }
                    _tpaPlugin.CoolDowns.Remove(player);
                }
            }

            if (_tpaPlugin.Requests.ContainsKey(requestTo))
            {
                if (_tpaPlugin.Requests[requestTo] == player)
                {
                    context.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_pending", Color.Red);
                    return;
                }
            }

            if (_tpaPlugin.Requests.ContainsKey(requestTo))
                _tpaPlugin.Requests[requestTo] = player;
            else
                _tpaPlugin.Requests.Add(requestTo, player);

            context.User.SendLocalizedMessage(_tpaPlugin.Translations, "request_sent", Color.Yellow, requestTo.Name);
            requestTo.GetUser().SendLocalizedMessage(_tpaPlugin.Translations, "request_sent_1", Color.Yellow, player.Name);

            if (_tpaPlugin.CoolDowns.ContainsKey(player))
                _tpaPlugin.CoolDowns[player] = DateTime.Now;
            else
                _tpaPlugin.CoolDowns.Add(player, DateTime.Now);
        }
    }
}