namespace PermanentUpgradesRevamped;

public static class DeliveryPauseRules
{
    // Native TruckUpdate starts at 0 and finishes its return at state 13.
    // Keep the iterator alive at a visit boundary; never cancel a loaded truck.
    public static bool ShouldWait(bool paused,int state,float returnProgress)
        =>paused&&(state==0||state==13&&returnProgress>=1);
}
