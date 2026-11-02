namespace PermanentUpgradesRevamped;
public sealed class DailyDeliveryTracker
{
    int day=int.MinValue;bool consumed;
    public bool IsFirst(int currentDay)=>currentDay!=day||!consumed;
    public void Complete(int currentDay){day=currentDay;consumed=true;}
    public void Reset(){day=int.MinValue;consumed=false;}
}
public static class FirstDeliveryRules
{
    public static int ExtraMonsters(int rank){if(rank<0||rank>10)throw new ArgumentOutOfRangeException(nameof(rank));return rank*5;}
    public static int SpawnCount(int normalCount,int rank){if(normalCount<0)throw new ArgumentOutOfRangeException(nameof(normalCount));return checked(normalCount+ExtraMonsters(rank));}
}
