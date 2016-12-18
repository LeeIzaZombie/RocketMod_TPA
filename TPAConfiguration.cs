using Rocket.API;

namespace RocketMod_TPA
{
    public class TPAConfiguration : IRocketPluginConfiguration
    {
        public bool TPACoolDown = false;
        public int TPACoolDownSeconds = 20;
        public bool TPADelay = false;
        public int TPADelaySeconds = 10;
        public bool CancelOnBleeding = false;
        public bool CancelOnHurt = false;
        public bool NinjaTP = false;
        public int NinjaEffectID = 45;
        public bool TPATeleportProtection = false;
        public int TPATeleportProtectionSeconds = 15;

        public void LoadDefaults()
        {
        }
    }
}
