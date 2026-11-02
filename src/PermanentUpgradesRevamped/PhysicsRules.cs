namespace PermanentUpgradesRevamped;
public static class PhysicsRules
{
    public static void ReducedImpulse(float x,float y,float z,out float dx,out float dy,out float dz)
    {
        dx=x*.3f;dy=y*.3f;dz=z*.3f;var length=MathF.Sqrt(dx*dx+dy*dy+dz*dz);
        if(length>4){dx*=4/length;dy*=4/length;dz*=4/length;}
    }
}
