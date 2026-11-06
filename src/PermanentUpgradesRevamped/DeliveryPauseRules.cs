namespace PermanentUpgradesRevamped;

public static class DeliveryPauseRules
{
    // Each native day starts a fresh TruckUpdate at state 0: its first visit
    // must always run. Only hold after that visit returns at state 13.
    // Keep the iterator alive without clearing the operator's pause setting.
    public static bool ShouldWait(bool paused,int state,float returnProgress)
        =>paused&&state==13&&returnProgress>=1;
}
