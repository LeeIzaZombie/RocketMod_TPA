using Rocket.API;
using Rocket.Core.Assets;

namespace RocketMod_TPA
{
    public class TPAConfiguration : IRocketPluginConfiguration
    {
        public int TPACoolDownSeconds, TPADelaySeconds, NinjaEffectID;
        public bool TPADelay, CancelOnBleeding, CancelOnHurt, TPACoolDown, NinjaTP;

        public void LoadDefaults()
        {
            this.TPACoolDown = false;
            this.TPACoolDownSeconds = 20;
            this.TPADelay = false;
            this.TPADelaySeconds = 10;
            this.CancelOnBleeding = false;
            this.CancelOnHurt = false;
            this.NinjaTP = false;
            this.NinjaEffectID = 45;
            //this.DoubleTapDelaySeconds = 1;
            //this.TPADoubleTap = true;
        }
    }
}
