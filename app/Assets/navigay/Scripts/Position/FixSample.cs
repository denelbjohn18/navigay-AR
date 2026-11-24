namespace Navigay.Position
{
    public readonly struct FixSample
    {
        public readonly bool HasFix;
        public readonly float AccuracyM;
        public readonly bool Inside;
        public readonly string BlockName;
        public readonly SnapFix Snap;

        public FixSample(bool hasFix, float accuracyM, bool inside, string blockName, SnapFix snap)
        {
            HasFix = hasFix;
            AccuracyM = accuracyM;
            Inside = inside;
            BlockName = blockName;
            Snap = snap;
        }
    }
}
