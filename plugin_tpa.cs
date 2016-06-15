using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocketMod_TPA
{
    public class PluginTPA : RocketPlugin<TPAConfiguration>
    {
        public static PluginTPA Instance;

        protected override void Load()
        {
            Instance = this;
        }

        public override TranslationList DefaultTranslations 
         { 
             get 
             { 
                 return new TranslationList 
                 { 
                     { "help_line_1", "TPA allows you to request a teleport to another player." }, 
                     { "help_line_2", "/tpa (playerName) - Sends a teleport request." }, 
                     { "help_line_3", "/tpa accept - Accepts your latest TPA request." }, 
                     { "help_line_4", "/tpa deny - Denys your latest TPA request." }, 
                     { "playerNotFound", "Could not find that player!" },
                     { "playerInCar", "Teleport failed, the player is in a car." },
                     { "YouInCar", "Teleport failed, you can't teleport in a car." },
                     { "nopermission_send", "You do not have permission to send TPA requests." }, 
                     { "nopermission_accept", "You do not have permission to accept TPA requests." }, 
                     { "nopermission_deny", "You do not have permission to deny TPA requests." }, 
                     { "error_cooldown", "You may only send requests every 10 seconds." }, 
                     { "request_accepted", "You've accepted the tpa request from: " }, 
                     { "request_denied", "You've denied the tpa request from: " }, 
                     { "request_accepted_1", "has accepted your tpa request!" }, 
                     { "request_denied_1", "has denied your tpa request!" },
                     { "request_sent", "You have sent a tpa request to: " }, 
                     { "request_sent_1", "has sent you a tpa request, you can use /tpa accept to let them teleport to you!" },
                     { "request_none", "You have no requests available!" }, 
                     { "request_pending", "You're already pending a tpa request to: " } 
                 }; 
             } 
         } 

    }

    public class TPAConfiguration : IRocketPluginConfiguration
    {
        public int TPACoolDownSeconds;
        public void LoadDefaults()
        {
            TPACoolDownSeconds = 10;
        }
    }
}
