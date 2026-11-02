namespace PermanentUpgradesRevamped;

// Shared by the native coroutine hook and the Next Day button: a blocked click must not queue a start.
public sealed class PreparationGate
{
    public bool Waiting {get;private set;}
    bool requested;
    public void Begin(){Waiting=true;requested=false;}
    public void RequestStart(bool blocked){if(Waiting&&!blocked)requested=true;}
    public bool TryFinish(bool blocked)
    {
        if(!Waiting||!requested||blocked)return false;
        Waiting=false;requested=false;return true;
    }
    public void Reset(){Waiting=false;requested=false;}
}
