namespace PermanentUpgradesRevamped;

public sealed class Upgrade
{
    public string Name,Unit,Description; public int Group;public double[] Values;public int[] Prices;
    public int Cap=>Prices.Length;
    public Upgrade(string name,string unit,int group,string description,double[] values,int[] prices)
    {if(values.Length!=prices.Length+1||prices.Any(p=>p<=0))throw new ArgumentException("Invalid progression table");Name=name;Unit=unit;Group=group;Description=description;Values=values;Prices=prices;}
}
public static class Balance
{
    static int[] Prices(int count,int first)=>Enumerable.Range(1,count).Select(r=>r*first).ToArray();
    static double[] Linear(int count,double start,double finish)=>Enumerable.Range(0,count+1).Select(r=>Math.Round(start+(finish-start)*r/count,4)).ToArray();
    static double[] Milestones(double first,double middle,double last)
    {var a=new double[21];a[0]=1;for(var r=1;r<=10;r++)a[r]=Math.Round(first+(middle-first)*(r-1)/9,4);for(var r=11;r<=20;r++)a[r]=Math.Round(middle+(last-middle)*(r-10)/10,4);return a;}
    public static readonly string[] Groups={"PROFITABILITY","GRINDERS","PLAYER","SPECIAL BONUSES"};
    public static readonly int[][] Columns={new[]{0,2,6,7},new[]{8,3,9,10},new[]{1,5,4,12,18,11},new[]{13,14,15,16,17,19}};
    public static readonly Upgrade[] Upgrades={
        new("Starting Cash","$",0,"Starting money granted when resuming after selling the factory. Buying a rank does not grant cash immediately.",new double[]{0,100,300,700,1500,3000,5000,8000,12000,18000,25000,40000,60000,85000,110000,150000,225000,325000,475000,700000,1000000},Prices(20,5)),
        new("Damage","×",2,"Permanent damage contribution, added to run skill-tree bonuses.",Milestones(1.15,2.5,8),Prices(20,10)),
        new("Can Value","×",0,"Permanent can-value contribution, added to run bonuses. Shiny value applies separately. Maximum permanent contribution: x10.",Milestones(1.1,2,10),Prices(20,10)),
        new("Processing Speed","×",1,"Speeds up grinder processing. The five machines must still be unlocked in the run skill tree.",Milestones(1.1,2,5),Prices(20,10)),
        new("Vacuum Storage","×",2,"Increases base vacuum storage while preserving run upgrades.",Milestones(1.25,3,6),Prices(20,5)),
        new("Movement Speed","%",2,"Permanent movement speed: +5% per rank, up to +60%.",Linear(12,0,60),Prices(12,5)),
        new("Shiny Chance","pp",0,"Adds 0.5 percentage points per rank. Random shiny chance is capped at 40%, including run bonuses. Guaranteed Shiny Shipments remain separate.",Linear(20,0,10),Prices(20,15)),
        new("Shiny Value","×",0,"Multiplies shiny can value from x1.10 to x2. Stacks with can value.",Linear(10,1,2),Prices(10,20)),
        new("Grinder Width","×",1,"Widens grinder openings, floor geometry and capture volumes. Capped at x2.25 to preserve walkways between pits.",new double[]{1,1.15,1.3,1.5,1.75,2,2.25},new[]{40,80,150,260,420,650}),
        new("Suction Range","m",1,"Rank I unlocks a 4 m suction field at 0.3 m/s. Routes dead bodies toward less congested active grinders. Living monsters are unaffected.",new double[]{0,4,6,8,10,12,15,18,21,24,26},Prices(10,10)),
        new("Suction Power","m/s",1,"Target body speed from 0.45 to 3 m/s, with gentle acceleration and gravity preserved. Requires Suction Range I.",new double[]{.3,.45,.65,.9,1.2,1.5,1.8,2.1,2.4,2.7,3},Prices(10,10)),
        new("Critical Chance","pp",2,"Adds 1 percentage point per rank. Final critical chance is capped at 50%, including run bonuses.",Linear(10,0,10),Prices(10,15)),
        new("Vacuum Fire Rate","×",2,"Vacuum ejection rate: +10% per rank, up to x2. Stacks with run fire-rate upgrades without changing their base values.",Linear(10,1,2),Prices(10,10)),
        new("Shiny Shipment","unlock",3,"Preserves the original Shiny Shipment special effect. One-time purchase.",new double[]{0,1},new[]{200}),
        new("Damaged Delivery","%",3,"New arrivals have 2% less maximum HP per rank, up to 20%.",Linear(10,0,20),Prices(10,20)),
        new("Starting Shockwave","%",3,"One shockwave on the first delivery each day: 20 / 35 / 50% of maximum HP. Permanent damage and critical bonuses do not amplify it.",new double[]{0,20,35,50},new[]{60,150,350}),
        new("First Blood","unlock",3,"Original First Blood effect: the first ten hits of the day are lethal. One-time purchase.",new double[]{0,1},new[]{200}),
        new("Death Touch","pp",3,"Adds 0.2 percentage points per rank. Final instant-death chance is capped at 5%, including run bonuses. First Blood remains a separate guaranteed effect.",Linear(5,0,1),Prices(5,50)),
        new("Player Ammo Capacity","×",2,"Player weapon reserve: +10% of base capacity per rank, up to x3. Added to run bonuses at the next normal rearm. Turrets and vacuum storage have separate upgrades.",Linear(20,1,3),Prices(20,10)),
        new("First Delivery Reinforcements","monsters",3,"Adds 5 monsters per rank to the first delivery each day, up to +50. Stacks with run delivery-size upgrades. Later deliveries keep their normal size.",Linear(10,0,50),Prices(10,25))
    };
    public static double Value(int index,int rank)=>Upgrades[index].Values[rank];
    public static int Price(int index,int rank)=>Upgrades[index].Prices[rank];
    public static bool CanPurchase(int index,int[] ranks)=>index!=10||ranks[9]>0;
    public static int Refund(int[] ranks){Validate(ranks);return ranks.Select((rank,i)=>Upgrades[i].Prices.Take(rank).Sum()).Sum();}
    public static string Display(int index,int rank)
    {
        var value=Value(index,rank);
        return Upgrades[index].Unit switch {
            "$"=>FormattableString.Invariant($"${value:N0}"),
            "×"=>FormattableString.Invariant($"×{value:0.##}"),
            "pp"=>FormattableString.Invariant($"+{value:0.#} pp"),
            "m"=>rank==0?"INACTIVE":FormattableString.Invariant($"{value:0.#} m"),
            "m/s"=>FormattableString.Invariant($"{value:0.##} m/s"),
            "unlock"=>rank==0?"LOCKED":"OWNED",
            "monsters"=>FormattableString.Invariant($"+{value:0} monsters"),
            _=>FormattableString.Invariant($"{(index is 12 or 14?"−":"+")}{value:0.#}%")
        };
    }
    public static void Validate(int[] ranks){if(ranks==null||ranks.Length!=Upgrades.Length||ranks.Where((r,i)=>r<0||r>Upgrades[i].Cap).Any()||ranks[10]>0&&ranks[9]==0)throw new InvalidDataException("Invalid PUR ranks; refusing to overwrite save.");}
}
