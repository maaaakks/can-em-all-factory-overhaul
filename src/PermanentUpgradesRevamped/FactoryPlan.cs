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
        new(10001,265,35000000,"turrets",3,"Turret III","Adds a third turret to the factory.",0,0),
        new(10002,10001,100000000,"turrets",4,"Turret IV","Adds a fourth turret to the factory.",0,1),
        new(10011,273,20000000,"ammo",2,"Reserve V","Reserve capacity: ×5 → ×7 compared with base capacity.",1,0),
        new(10012,10011,40000000,"ammo",5,"Reserve VI","Reserve capacity: ×7 → ×10.",1,1),
        new(10013,10012,80000000,"ammo",10,"Reserve VII","Reserve capacity: ×10 → ×15.",1,2),
        new(10014,10013,160000000,"ammo",20,"Reserve VIII","Reserve capacity: ×15 → ×25. Additional reserves are filled at the next normal rearm.",1,3),

    };
    public static FactoryUpgrade Find(int id)=>All.FirstOrDefault(u=>u.Id==id);
    public static double Target(string kind,ISet<int> owned,double fallback)=>All.Where(u=>u.Kind==kind&&owned.Contains(u.Id)).Select(u=>u.Value).DefaultIfEmpty(fallback).Max();
}
