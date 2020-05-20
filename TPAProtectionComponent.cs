using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace RocketMod_TPA
{
    public class TPAProtectionComponent : UnturnedPlayerComponent
    {
        private bool protect = false;
        internal bool LoginProtection = false;
        internal DateTime LoginProtectionStart;
        private bool teleportProtection = false;
        private DateTime teleportProtectionStart;
        private bool doingDelayTP = false;
        private DateTime delayTPSartTime;
        private Vector3 curLocDelayTP;
        internal int delay;
        internal DateTime lastCooldownStart;
        private UnturnedPlayer target = null;
        internal List<CSteamID> TPARequestList = new List<CSteamID>();
        private byte health;
        private byte delayTPHealth;
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

        internal void RunTeleportProtections()
        {
            UnturnedPlayerFeatures features = Player.GetComponent<UnturnedPlayerFeatures>();
            // Don't execute if the player already has god mode enabled.
            if (!features.GodMode && (!Protected || (Protected && LoginProtection)))
            {
                if (!LoginProtection)
                    Protected = true;
                else
                    LoginProtection = false;
                teleportProtection = true;
                teleportProtectionStart = DateTime.Now;
                UnturnedChat.Say(Player, PluginTPA.Instance.Translate("teleport_protection_enabled", PluginTPA.Instance.Configuration.Instance.TPATeleportProtectionSeconds), Color.yellow);
            }
        }

        // to be ran on the requesting player PC.
        internal void DelayTP(UnturnedPlayer target)
        {
            delay = PluginTPA.Instance.Configuration.Instance.TPADelaySeconds;
            UnturnedChat.Say(Player, PluginTPA.Instance.Translate("request_accepted_2", target.CharacterName, delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);
            UnturnedChat.Say(target, PluginTPA.Instance.Translate("request_accepted_3", Player.CharacterName, delay, PluginTPA.Instance.Translate("Seconds")), Color.yellow);

            this.target = target;
            delayTPHealth = Player.Health;
            doingDelayTP = true;
            delayTPSartTime = DateTime.Now;
            curLocDelayTP = Player.Position;


        }

        // to be ran on the requesting player PC.
        internal void TPplayer(UnturnedPlayer target)
        {
            if (PluginTPA.Instance.Configuration.Instance.NinjaTP)
                EffectManager.sendEffect(PluginTPA.Instance.Configuration.Instance.NinjaEffectID, 30, Player.Position);
            if (!Player.Player.teleportToLocation(target.Position, target.Rotation))
            {
                if (Player.IsAdmin)
                {
                    Player.Player.teleportToLocationUnsafe(target.Position, target.Rotation);
                    return;
                }
                UnturnedChat.Say(Player, PluginTPA.Instance.Translate("tpa_fail_obstructed"));
                UnturnedChat.Say(target, PluginTPA.Instance.Translate("tpa_fail_obstructed"));
                return;
            }
            Logger.Log(string.Format("Player: {0} [{1}] ({2}), has TPA'd to player: {3} [{4}] ({5}), at location: {6}.", Player.CharacterName, Player.SteamName, Player.CSteamID, target.CharacterName, target.SteamName, target.CSteamID, target.Player.transform.position));
        }

        private void OnDestroy()
        {
            if (Protected)
                Protected = false;
            TPARequestList.Clear();
            if (doingDelayTP)
            {
                if (target != null && !target.CSteamID.IsInvalid())
                {
                    UnturnedChat.Say(target, PluginTPA.Instance.Translate("error_player_left_server"), Color.red);
                }
            }
        }

        public void FixedUpdate()
        {
            if (PluginTPA.Instance.State == PluginState.Loaded)
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
                if (PluginTPA.Instance.Configuration.Instance.TPATeleportProtection && Protected && teleportProtection)
                {
                    if ((DateTime.Now - teleportProtectionStart).TotalSeconds > PluginTPA.Instance.Configuration.Instance.TPATeleportProtectionSeconds)
                    {
                        teleportProtection = false;
                        Protected = false;
                        UnturnedChat.Say(Player, PluginTPA.Instance.Translate("teleport_protection_disabled"), Color.yellow);
                    }
                }

                if (doingDelayTP)
                {
                    // Check to see if the players are still on the server. Run cleanup and return if they aren't.
                    if (target == null || target.CSteamID.IsInvalid())
                    {
                        UnturnedChat.Say(Player, PluginTPA.Instance.Translate("error_player_left_server"), Color.red);
                        doingDelayTP = false;
                        return;
                    }
                    if (Player.Stance == EPlayerStance.DRIVING || Player.Stance == EPlayerStance.SITTING)
                    {
                        doingDelayTP = false;
                        UnturnedChat.Say(Player, PluginTPA.Instance.Translate("error_incar"), Color.red);
                        UnturnedChat.Say(target, PluginTPA.Instance.Translate("error_incar1", Player.CharacterName), Color.red);
                        return;
                    }
                    if ((DateTime.Now - delayTPSartTime).TotalSeconds < PluginTPA.Instance.Configuration.Instance.TPADelaySeconds)
                    {
                        if (PluginTPA.Instance.Configuration.Instance.CancelOnBleeding && Player.Bleeding)
                        {
                            doingDelayTP = false;
                            UnturnedChat.Say(Player, PluginTPA.Instance.Translate("error_bleeding"), Color.red);
                            UnturnedChat.Say(target, PluginTPA.Instance.Translate("error_bleeding1", Player.CharacterName), Color.red);
                            return;
                        }
                        if (PluginTPA.Instance.Configuration.Instance.CancelOnHurt && delayTPHealth > Player.Health)
                        {
                            doingDelayTP = false;
                            UnturnedChat.Say(Player, PluginTPA.Instance.Translate("error_hurt"), Color.red);
                            UnturnedChat.Say(target, PluginTPA.Instance.Translate("error_hurt1", Player.CharacterName), Color.red);
                            return;
                        }
                        if (PluginTPA.Instance.Configuration.Instance.CancelOnMoved && Vector3.Distance(Player.Position, curLocDelayTP) > PluginTPA.Instance.Configuration.Instance.MaxAllowedMoveDistance)
                        {
                            doingDelayTP = false;
                            UnturnedChat.Say(Player, PluginTPA.Instance.Translate("error_movedtoomuch"), Color.red);
                            UnturnedChat.Say(target, PluginTPA.Instance.Translate("error_movedtoomuch1", Player.CharacterName), Color.red);
                            return;
                        }
                    }
                    else
                    {
                        doingDelayTP = false;
                        UnturnedChat.Say(Player, PluginTPA.Instance.Translate("request_success"), Color.yellow);
                        TPplayer(target);
                        if (PluginTPA.Instance.Configuration.Instance.TPATeleportProtection)
                            RunTeleportProtections();
                    }
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
