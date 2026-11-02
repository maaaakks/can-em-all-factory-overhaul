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
    public static readonly string[] Groups={"RENTABILITÉ","BROYEURS","PERSONNEL","BONUS SPÉCIAUX"};
    public static readonly int[][] Columns={new[]{0,2,6,7},new[]{8,3,9,10},new[]{1,5,4,12,18,11},new[]{13,14,15,16,17}};
    public static readonly Upgrade[] Upgrades={
        new("Capital de départ","$",0,"Argent accordé après la vente de l'usine, lors de la reprise. Le prochain achat ne verse pas d'argent immédiatement.",new double[]{0,100,300,700,1500,3000,5000,8000,12000,18000,25000,40000,60000,85000,110000,150000,225000,325000,475000,700000,1000000},Prices(20,5)),
        new("Dégâts","×",2,"Bonus permanent aux dégâts, ajouté aux bonus de l'arbre de partie.",Milestones(1.15,2.5,8),Prices(20,10)),
        new("Valeur des conserves","×",0,"Bonus permanent à la valeur des conserves, ajouté aux bonus de partie. Les bonus shiny s'appliquent séparément. Maximum permanent ×10.",Milestones(1.1,2,10),Prices(20,10)),
        new("Cadence de traitement","×",1,"Accélère le traitement des broyeurs. Les cinq machines restent à débloquer dans l'arbre de partie.",Milestones(1.1,2,5),Prices(20,10)),
        new("Stockage du vacuum","×",2,"Augmente la capacité de base du canon aspirateur, en conservant les achats de partie.",Milestones(1.25,3,6),Prices(20,5)),
        new("Déplacement","%",2,"Bonus permanent de déplacement. +5 % par rang, jusqu'à +60 %.",Linear(12,0,60),Prices(12,5)),
        new("Probabilité shiny","pp",0,"+0,5 point de pourcentage par rang. Probabilité aléatoire finale plafonnée à 40 %, arbre compris. Les livraisons shiny garanties restent spéciales.",Linear(20,0,10),Prices(20,15)),
        new("Valeur des shiny","×",0,"Multiplie la valeur des conserves shiny : ×1,10 à ×2. Se cumule avec la valeur des conserves.",Linear(10,1,2),Prices(10,20)),
        new("Largeur des broyeurs","×",1,"Élargit les ouvertures, le sol et la capture physique. Plafond ×2,25 pour conserver des passages entre les fosses.",new double[]{1,1.15,1.3,1.5,1.75,2,2.25},new[]{40,80,150,260,420,650}),
        new("Portée d'aspiration","m",1,"Le premier rang débloque l'aspiration des corps morts à 0,3 m/s. Répartit les corps vers les broyeurs actifs moins chargés. Aucun effet sur les vivants.",new double[]{0,4,6,8,10,12,15,18,21,24,26},Prices(10,10)),
        new("Puissance d'aspiration","m/s",1,"Vitesse cible des corps : 0,45 à 3 m/s, accélération douce et gravité conservée. Nécessite Portée d'aspiration I.",new double[]{.3,.45,.65,.9,1.2,1.5,1.8,2.1,2.4,2.7,3},Prices(10,10)),
        new("Critiques","pp",2,"+1 point de pourcentage par rang. Chances finales plafonnées à 50 %, bonus de partie compris.",Linear(10,0,10),Prices(10,15)),
        new("Cadence du vacuum","×",2,"Cadence d’éjection du vacuum : +10 % par rang, jusqu’à ×2. Se cumule avec la cadence achetée dans l’arbre, sans modifier sa valeur de base.",Linear(10,1,2),Prices(10,10)),
        new("Livraison shiny","unlock",3,"Conserve l'effet spécial Shiny Shipment d'origine. Achat unique.",new double[]{0,1},new[]{200}),
        new("Livraison fragilisée","%",3,"Les nouvelles créatures arrivent avec 2 % de leurs PV maximum en moins par rang, jusqu'à 20 %.",Linear(10,0,20),Prices(10,20)),
        new("Onde de départ","%",3,"Une impulsion sur la première livraison de chaque journée : 20 / 35 / 50 % des PV maximum. Aucun cumul de critiques ou de dégâts permanents.",new double[]{0,20,35,50},new[]{60,150,350}),
        new("Premier sang","unlock",3,"Effet First Blood natif : les dix premières touches de la journée sont mortelles. Achat unique.",new double[]{0,1},new[]{200}),
        new("Toucher mortel","pp",3,"+0,2 point de pourcentage par rang. Probabilité finale plafonnée à 5 %, arbre compris. Premier sang reste un effet garanti distinct.",Linear(5,0,1),Prices(5,50)),
        new("Munitions du joueur","×",2,"Réserve des armes du joueur : +10 % de la capacité de base par rang, jusqu’à ×3. Bonus ajouté à ceux de l’arbre, appliqué au prochain réarmement. Les tourelles et le stockage du vacuum ont leurs propres améliorations.",Linear(20,1,3),Prices(20,10))
    };
    public static double Value(int index,int rank)=>Upgrades[index].Values[rank];
    public static int Price(int index,int rank)=>Upgrades[index].Prices[rank];
    public static bool CanPurchase(int index,int[] ranks)=>index!=10||ranks[9]>0;
    public static int Refund(int[] ranks){Validate(ranks);return ranks.Select((rank,i)=>Upgrades[i].Prices.Take(rank).Sum()).Sum();}
    public static string Display(int index,int rank)
    {var value=Value(index,rank);return Upgrades[index].Unit switch {"$"=>$"{value:N0} $","×"=>$"×{value:0.##}","pp"=>$"+{value:0.#} pts %","m"=>rank==0?"INACTIF":$"{value:0.#} m","m/s"=>$"{value:0.##} m/s","unlock"=>rank==0?"À DÉBLOQUER":"ACQUIS",_=>$"{(index is 12 or 14?"−":"+")}{value:0.#} %"};}
    public static void Validate(int[] ranks){if(ranks==null||ranks.Length!=Upgrades.Length||ranks.Where((r,i)=>r<0||r>Upgrades[i].Cap).Any()||ranks[10]>0&&ranks[9]==0)throw new InvalidDataException("Invalid PUR ranks; refusing to overwrite save.");}
}
