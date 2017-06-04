using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RocketMod_TPA
{
    public class TPAProtectionComponent : UnturnedPlayerComponent
    {
        private bool protect = false;
        internal bool LoginProtection = false;
        internal DateTime LoginProtectionStart;
        private byte health;
        private byte water;
        private byte food;
        private byte virus;

        // Based off of the god code in Rocket's UnturnedPlayerFeatures class.
        internal bool Protected
        {
            get
            {
                return protect;
            }
            set
            {
                if (value)
                {
                    Player.Events.OnUpdateHealth += Events_OnUpdateHealth;
                    Player.Events.OnUpdateWater += Events_OnUpdateWater;
                    Player.Events.OnUpdateFood += Events_OnUpdateFood;
                    Player.Events.OnUpdateVirus += Events_OnUpdateVirus;
                    water = Player.Thirst;
                    food = Player.Hunger;
                    virus = Player.Infection;
                    health = Player.Health;
                }
                else
                {
                    Player.Events.OnUpdateHealth -= Events_OnUpdateHealth;
                    Player.Events.OnUpdateWater -= Events_OnUpdateWater;
                    Player.Events.OnUpdateFood -= Events_OnUpdateFood;
                    Player.Events.OnUpdateVirus -= Events_OnUpdateVirus;
                    Player.Bleeding = false;
                }
                protect = value;
            }
        }

        internal void EventCleanup()
        {
            if (Protected)
                Protected = false;
        }

        public void FixedUpdate()
        {
            if (PluginTPA.Instance.Configuration.Instance.UseLoginProtection && Protected && LoginProtection)
            {
                if ((DateTime.Now - LoginProtectionStart).TotalSeconds > PluginTPA.Instance.Configuration.Instance.LoginProtectionTime)
                {
                    LoginProtection = false;
                    Protected = false;
                    UnturnedChat.Say(Player, PluginTPA.Instance.Translate("login_protection_disabled", PluginTPA.Instance.Configuration.Instance.LoginProtectionTime), UnityEngine.Color.yellow);
                }
            }
        }

        private void Events_OnUpdateVirus(UnturnedPlayer player, byte virus)
        {
            if (virus < this.virus && virus < 95)
                Player.Infection = (byte)(100 - this.virus);
        }

        private void Events_OnUpdateFood(UnturnedPlayer player, byte food)
        {
            if (food < this.food && food < 95)
                Player.Hunger = (byte)(100 - this.food);
        }

        private void Events_OnUpdateWater(UnturnedPlayer player, byte water)
        {
            if (water < this.water && water < 95)
                Player.Thirst = (byte)(100 - this.water);
        }

        private void Events_OnUpdateHealth(UnturnedPlayer player, byte health)
        {
            if (health < this.health && health < 95)
                Player.Heal((byte)(this.health - health));
            Player.Bleeding = false;
        }
    }
}
