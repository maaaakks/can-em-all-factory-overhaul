namespace PermanentUpgradesRevamped;

public sealed class FactoryUpgrade
{
    public int Id, Parent, Price, Row, Column;
    public string Name, Description, Kind;
    public double Value;
    public FactoryUpgrade(int id,int parent,int price,string kind,double value,string name,string description,int row,int column)
        =>(Id,Parent,Price,Kind,Value,Name,Description,Row,Column)=(id,parent,price,kind,value,name,description,row,column);
}

public static class FactoryPlan
{
    // IDs are stable saved identifiers; never reuse an ID for a different effect.
    public static readonly FactoryUpgrade[] All={
        new(10001,265,35000000,"turrets",3,"Tourelle III","Ajoute une troisième tourelle dans l'usine.",0,0),
        new(10002,10001,100000000,"turrets",4,"Tourelle IV","Ajoute une quatrième tourelle dans l'usine.",0,1),
        new(10011,273,20000000,"ammo",2,"Réserve V","Réserve globale : ×5 → ×7 par rapport à la capacité de base.",1,0),
        new(10012,10011,40000000,"ammo",5,"Réserve VI","Réserve globale : ×7 → ×10.",1,1),
        new(10013,10012,80000000,"ammo",10,"Réserve VII","Réserve globale : ×10 → ×15.",1,2),
        new(10014,10013,160000000,"ammo",20,"Réserve VIII","Réserve globale : ×15 → ×25. Les réserves supplémentaires sont remplies au prochain réarmement.",1,3),

    };
    public static FactoryUpgrade Find(int id)=>All.FirstOrDefault(u=>u.Id==id);
    public static double Target(string kind,ISet<int> owned,double fallback)=>All.Where(u=>u.Kind==kind&&owned.Contains(u.Id)).Select(u=>u.Value).DefaultIfEmpty(fallback).Max();
}
