namespace PermanentUpgradesRevamped;

// Native SaveData identity can also change after Ending. Only explicit New Game
// clears permanent ranks; a different native wrapper is not evidence of a new slot.
public sealed class SaveScopedProgress
{
    public int[] Ranks=new int[Balance.Upgrades.Length];
    public bool Migrated;
    long? boundSave;
    public void Restore(int[] ranks,bool migrated)
    {
        Balance.Validate(ranks);Ranks=(int[])ranks.Clone();Migrated=migrated;boundSave=null;
    }
    public void StartNew(){Ranks=new int[Balance.Upgrades.Length];Migrated=true;boundSave=null;}
    public void BindLoaded(long identity){boundSave=identity;}
    public bool BindCurrent(long identity)
    {
        if(boundSave.HasValue&&boundSave.Value!=identity){boundSave=identity;return true;}
        boundSave=identity;return false;
    }
}
