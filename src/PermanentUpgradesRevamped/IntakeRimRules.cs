namespace PermanentUpgradesRevamped;

public static class IntakeRimRules
{
    public static bool Supported(float floor,float minimum,float maximum)
        =>float.IsFinite(floor)&&float.IsFinite(minimum)&&float.IsFinite(maximum)
          &&minimum<floor-.5f&&maximum>floor&&maximum<=floor+.15f;
    // Only the raised lip changes. Lower pit walls and floor-level vertices stay intact.
    public static float FlushHeight(float height,float floor)=>Math.Min(height,floor);
}
