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
        public bool CancelOnMoved = false;
        public float MaxAllowedMoveDistance = 5.0f;

        public bool NinjaTP = false;
        public ushort NinjaEffectID = 45;
        public bool TPATeleportProtection = false;
        public int TPATeleportProtectionSeconds = 15;
        public bool UseLoginProtection = false;
        public int LoginProtectionTime = 30;

        public void LoadDefaults()
        {
        }
    }
}
