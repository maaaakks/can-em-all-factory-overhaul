namespace PermanentUpgradesRevamped;
public static class EffectsRules
{
    public static float Chance(float native,float bonus,float cap)=>Math.Clamp(native+bonus,0,cap);
    public static float PerRoll(float native,float bonus,float cap,float multiplier)
    {
        var probability=Chance(native,bonus,cap);
        if(multiplier<=0)return 0;
        var effective=1-Math.Pow(1-probability,multiplier);
        return effective<=cap?probability:(float)(1-Math.Pow(1-cap,1/multiplier));
    }
}
