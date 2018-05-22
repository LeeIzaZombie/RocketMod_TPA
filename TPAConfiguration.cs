namespace RocketMod_TPA
{
    public class TpaConfiguration
    {
        public uint TpaCoolDownSeconds { get; set; } = 20;
        public uint TpaDelaySeconds { get; set; } = 10;
        public bool CancelTpaOnHurt { get; set; } = false;
        public bool TpaCoolDown { get; set; } = false;
    }
}
