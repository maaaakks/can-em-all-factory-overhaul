namespace PermanentUpgradesRevamped;

public static class SuctionRules
{
    // Queue cost must keep growing: capping it makes all large queues indistinguishable.
    public static float RouteScore(float distance,int queued,bool previous)
        =>distance+Math.Max(0,queued)*1.5f-(previous?2:0);
    public static bool Eligible(bool active,bool dead,bool held,bool falling,bool kinematic,bool includeLiving)
        =>active&&(dead||includeLiving)&&!held&&!falling&&!kinematic;
    public static void Acceleration(float vx,float vz,float tx,float tz,out float ax,out float az)
    {
        ax=(tx-vx)*2;az=(tz-vz)*2;
        var magnitude=MathF.Sqrt(ax*ax+az*az);
        if(magnitude>2.4f){ax*=2.4f/magnitude;az*=2.4f/magnitude;}
    }
}
