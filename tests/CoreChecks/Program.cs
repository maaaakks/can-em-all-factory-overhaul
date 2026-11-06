using PermanentUpgradesRevamped;
using Newtonsoft.Json.Linq;


void Check(bool c,string m){if(!c)throw new Exception(m);Console.WriteLine("PASS "+m);}
Check(Balance.Upgrades.Length==20&&Balance.Columns.SelectMany(c=>c).Order().SequenceEqual(Enumerable.Range(0,20)),"20 unique rows, all visible once");
for(var col=0;col<4;col++)Check(Balance.Columns[col].Length<=6&&Balance.Columns[col].All(i=>Balance.Upgrades[i].Group==col),"Column "+col+" group consistency and at most six rows");
Check(Balance.Value(12,0)==1&&Balance.Value(12,10)==2&&Balance.Upgrades[12].Prices.Sum()==550,"Vacuum cadence baseline 1x, maximum 2x, total 550 points");
Check(Balance.Value(18,0)==1&&Balance.Value(18,20)==3&&Balance.Upgrades[18].Prices.Sum()==2100,"Player ammo baseline 1x, maximum 3x, total 2100 points");
for(var rank=0;rank<=10;rank++){
 var old=new int[18];old[0]=20;old[8]=3;old[9]=9;old[10]=9;old[12]=rank;
 var source=JObject.FromObject(new{upgradePoints=1000,money=123456L,totalMoney=98765L,unlockedSkills=new[]{1,2,10001},pur=new{schema=4,migrated=true,ranks=old}});
 var next=SaveMigration.Upgrade(source,out var refund,out var cash);
 var ranks=next["pur"]["ranks"].ToObject<int[]>();
 Check(refund==10*rank*(rank+1)/2&&cash==0&&(int)next["upgradePoints"]==1000+refund,"Exact endurance refund rank "+rank);
 Check(ranks.Length==20&&ranks[12]==0&&ranks[18]==0&&Enumerable.Range(0,18).Where(i=>i!=12).All(i=>ranks[i]==old[i]),"Only endurance replaced; every other permanent rank retained");
 var again=SaveMigration.Upgrade(next,out var refund2);Check(refund2==0&&JToken.DeepEquals(next,again),"Schema 6 restart cannot refund twice");
 Check((long)next["money"]==123456&&(long)next["totalMoney"]==98765&&JToken.DeepEquals(source["unlockedSkills"],next["unlockedSkills"]),"Run money, earned revenue and run skills preserved");
}
var legacy=JObject.FromObject(new{upgradePoints=0,pur=new{schema=3,migrated=true,ranks=new[]{12,12,12,9,6,6},specials=Enumerable.Repeat(true,8).ToArray()}});var converted=SaveMigration.Upgrade(legacy,out var legacyRefund);Check(legacyRefund==3227&&(int)converted["pur"]["schema"]==6&&converted["pur"]["ranks"].ToObject<int[]>().Length==20,"Old six-rank schema migrates directly to latest schema");
Check(Balance.Refund(Balance.Upgrades.Select(u=>u.Cap).ToArray())==23400,"New complete permanent budget 23,400 points");
Console.WriteLine("ALL LOADOUT CHECKS PASSED");

Check(Balance.Value(2,10)==2&&Balance.Value(2,15)==6&&Balance.Value(2,20)==10&&Balance.Upgrades[2].Prices.Sum()==2100,"Income revised: rank 10 x2, rank 15 x6, rank 20 x10, same 2100-point total");

var graph=new[]{new BulkNode{Id=1,Price=0},new BulkNode{Id=2,Price=1000,Requires=new[]{1}},new BulkNode{Id=3,Price=1,Requires=new[]{2}},new BulkNode{Id=4,Price=1500,Requires=new[]{1}},new BulkNode{Id=5,Price=100000,Requires=new[]{3}},new BulkNode{Id=6,Price=100,Excluded=true},new BulkNode{Id=7,Price=1,Requires=new[]{99}}};
var plan=BulkPlan.Build(graph,2501);
Check(plan.Ids.SequenceEqual(new[]{1,2,3,4})&&plan.Cost==2501,"Rescan after each purchase prioritizes a newly unlocked cheap child; exact budget spent");
Check(BulkPlan.Build(graph,999).Cost==0&&BulkPlan.Build(graph,0).Ids.SequenceEqual(new[]{1}),"Insufficient and zero budgets never overspend; free prerequisites still included");
Check(BulkPlan.Build(graph,1000000).Ids.Contains(5),"No per-node price ceiling: expensive upgrades included when affordable");
Check(BulkPlan.Build(graph.Reverse(),2501).Ids.SequenceEqual(plan.Ids),"Source tree ordering cannot change the purchase order");
Check(BulkPlan.Build(new[]{new BulkNode{Id=1,Price=2000000000},new BulkNode{Id=2,Price=2000000000}},4000000000L).Cost==4000000000L,"Large budgets use 64-bit totals");
var random=new Random(81);
for(var run=0;run<250;run++){
 var nodes=Enumerable.Range(1,100).Select(id=>new BulkNode{Id=id,Price=random.Next(0,100000),Purchased=random.NextDouble()<.1,Excluded=random.NextDouble()<.05,Requires=id>1&&random.NextDouble()<.7?new[]{random.Next(1,id)}:Array.Empty<int>()}).ToArray();
 var budget=(long)random.Next(0,2000000);var result=BulkPlan.Build(nodes,budget);var owned=nodes.Where(n=>n.Purchased).Select(n=>n.Id).ToHashSet();var remaining=budget;
 foreach(var id in result.Ids){var expected=nodes.Where(n=>!n.Purchased&&!n.Excluded&&!owned.Contains(n.Id)&&n.Price<=remaining&&n.Requires.All(owned.Contains)).OrderBy(n=>n.Price).ThenBy(n=>n.Id).First();if(expected.Id!=id)throw new Exception("Not the cheapest accessible upgrade");remaining-=expected.Price;owned.Add(id);}
 if(result.Cost!=budget-remaining||remaining<0||nodes.Any(n=>!n.Excluded&&!owned.Contains(n.Id)&&n.Price<=remaining&&n.Requires.All(owned.Contains)))throw new Exception("Invalid budget or premature stop");
}
Console.WriteLine("PASS 250 generated dependency graphs: cheapest eligible purchase each step, no overspend, no affordable eligible node left");

random=new Random(9);
for(var i=0;i<10000;i++){
 var x=(float)(random.NextDouble()*200-100);var y=(float)(random.NextDouble()*200-100);var z=(float)(random.NextDouble()*200-100);
 PhysicsRules.ReducedImpulse(x,y,z,out var dx,out var dy,out var dz);
 var length=Math.Sqrt(dx*dx+dy*dy+dz*dz);var original=Math.Sqrt(x*x+y*y+z*z);
 if(length>4.00001||Math.Abs(length-Math.Min(4,original*.3))>.00002||dx*x+dy*y+dz*z<0)throw new Exception("Invalid weapon impulse reduction");
}
Console.WriteLine("PASS 10,000 weapon impulse vectors: 30% of added velocity, capped at 4 m/s, original impulse direction preserved");
PhysicsRules.ReducedImpulse(0,0,0,out var a,out var b,out var c);if(a!=0||b!=0||c!=0)throw new Exception("Spurious impulse");
Console.WriteLine("PASS zero damage impulse does not change existing motion; no damage values or throw methods patched");

for(var multiplier=.1f;multiplier<=10;multiplier+=.1f)foreach(var cap in new[]{.05f,.5f}){
    var chance=EffectsRules.PerRoll(.9f,.1f,cap,multiplier);
    if(1-Math.Pow(1-chance,multiplier)>cap+.000001)throw new Exception("Effective probability cap exceeded");
}
Check(EffectsRules.Chance(.05f,.1f,.4f)==.15f,"Shiny bonus uses percentage points");
Check(!SuctionRules.Eligible(true,false,false,false,false,false)&&SuctionRules.Eligible(true,true,false,false,false,false),"Suction eligibility preserves living creatures");
Check(SuctionRules.RouteScore(10,238,false)>SuctionRules.RouteScore(23,8,false),"Congested centre loses to an available side grinder");
var oldRobots=JObject.FromObject(new{money=0L,totalMoney=100L,unlockedSkills=RobotRetirement.Prices.Keys.Concat(new[]{1,10001}).ToArray()});
var retired=RobotRetirement.Upgrade(oldRobots,out var robotRefund);
Check(robotRefund==261275000L&&(long)retired["totalMoney"]==100,"Robot refund preserves earned-revenue statistics");
var twice=RobotRetirement.Upgrade(retired,out var twiceRefund);Check(twiceRefund==0&&JToken.DeepEquals(retired,twice),"Robot retirement is idempotent");
Console.WriteLine("ALL CORE CHECKS PASSED");

Check(Balance.Upgrades[19].Group==3&&Balance.Upgrades[19].Cap==10&&Balance.Upgrades[19].Prices.Sum()==1375,"First delivery: special bonus, ten ranks, 1375-point total");
for(var rank=0;rank<=10;rank++)Check(Balance.Value(19,rank)==rank*5&&FirstDeliveryRules.ExtraMonsters(rank)==rank*5&&FirstDeliveryRules.SpawnCount(60,rank)==60+rank*5,"First delivery adds exactly five monsters per rank "+rank);
var daily=new DailyDeliveryTracker();Check(daily.IsFirst(4),"First spawn of a day is eligible");
daily.Complete(4);Check(!daily.IsFirst(4),"Later deliveries cannot receive the bonus again");
Check(daily.IsFirst(5),"Next day makes a new first delivery eligible");
daily.Complete(5);daily.Reset();Check(daily.IsFirst(5),"Factory reset re-arms the bonus even when the day number is reused");
var uncompleted=new DailyDeliveryTracker();Check(uncompleted.IsFirst(1)&&uncompleted.IsFirst(1),"An unsuccessful spawn does not consume the bonus before Complete");
var previousRanks=Balance.Upgrades.Take(19).Select(u=>u.Cap).ToArray();
var schema5=JObject.FromObject(new{money=200L,upgradePoints=1000,pur=new{schema=5,migrated=true,ranks=previousRanks},unlockedSkills=new[]{1,2,10001}});
var schema6=SaveMigration.Upgrade(schema5,out var newRefund,out var cashRefund);
var expanded=schema6["pur"]["ranks"].ToObject<int[]>();
Check(newRefund==0&&cashRefund==0&&(int)schema6["upgradePoints"]==1000&&expanded.Length==20&&expanded[19]==0&&expanded.Take(19).SequenceEqual(previousRanks),"Schema 5 migration preserves all purchases/points and appends an unpurchased bonus");
Check((int)schema5["pur"]["schema"]==5&&(int)schema6["pur"]["schema"]==6,"Migration clones its input and records schema 6");
var repeated=SaveMigration.Upgrade(schema6,out var repeatedRefund);Check(repeatedRefund==0&&JToken.DeepEquals(schema6,repeated),"New bonus migration is idempotent");
try{FirstDeliveryRules.ExtraMonsters(11);throw new Exception("Over-cap first-delivery rank accepted");}catch(ArgumentOutOfRangeException){}
Console.WriteLine("ALL FIRST-DELIVERY CHECKS PASSED");

var previousCulture=System.Globalization.CultureInfo.CurrentCulture;
try{
 System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
 Check(Balance.Display(0,20)=="$1,000,000"&&Balance.Display(8,1)=="×1.15"&&Balance.Display(6,1)=="+0.5 pp","English permanent numbers do not inherit French Windows formatting");
 Check(Balance.Display(9,0)=="INACTIVE"&&Balance.Display(13,0)=="LOCKED"&&Balance.Display(13,1)=="OWNED"&&Balance.Display(19,10)=="+50 monsters","English permanent status and delivery labels");
 Check(Balance.Upgrades.All(u=>u.Name.All(c=>c<128)&&u.Description.All(c=>c<128))&&FactoryPlan.All.All(u=>u.Name.All(c=>c<128)&&u.Description.All(c=>c<128||c=='×'||c=='→')),"All custom permanent/run upgrade names and descriptions use English text and math symbols");
}finally{System.Globalization.CultureInfo.CurrentCulture=previousCulture;}
Console.WriteLine("ALL ENGLISH UI CHECKS PASSED");

var preparation=new PreparationGate();
Check(!preparation.TryFinish(false),"Idle preparation cannot release an unstarted day");
preparation.Begin();Check(preparation.Waiting&&!preparation.TryFinish(false),"Opening the shop waits for an explicit start");
preparation.RequestStart(true);Check(!preparation.TryFinish(false),"A click in a blocking panel cannot queue a delayed day start");
preparation.RequestStart(false);Check(!preparation.TryFinish(true)&&preparation.Waiting,"The day cannot start while the prestige/pause panel is open");
Check(preparation.TryFinish(false)&&!preparation.Waiting&&!preparation.TryFinish(false),"Explicit NEXT DAY releases preparation exactly once");
preparation.Begin();preparation.RequestStart(false);preparation.Reset();Check(!preparation.Waiting&&!preparation.TryFinish(false),"Scene reset drops any pending start request");
preparation.Begin();Check(preparation.Waiting&&!preparation.TryFinish(false),"A new game can prepare again without inheriting the old request");
Console.WriteLine("ALL SHOP PREPARATION CHECKS PASSED");

var scoped=new SaveScopedProgress();
var completed=Balance.Upgrades.Select(u=>u.Cap).ToArray();
scoped.Restore(completed,true);scoped.BindLoaded(101);
Check(!scoped.BindCurrent(101)&&scoped.Ranks.SequenceEqual(completed),"Continue binds loaded ranks to the restored native save");
scoped.StartNew();Check(scoped.Migrated&&scoped.Ranks.All(rank=>rank==0),"New Game clears every permanent rank, including grinders and suction");
scoped.BindCurrent(101);scoped.BindCurrent(202);
Check(scoped.Ranks.All(rank=>rank==0)&&completed.SequenceEqual(Balance.Upgrades.Select(u=>u.Cap)),"Native replacement cannot inherit ranks or mutate the previous save snapshot");
scoped.Restore(completed,true);scoped.BindLoaded(303);
Check(!scoped.BindCurrent(303)&&scoped.Ranks.SequenceEqual(completed),"Continue after New Game can still restore the ranks stored in that save");
Check(scoped.BindCurrent(404)&&scoped.Migrated&&scoped.Ranks.SequenceEqual(completed),"Ending continuation replaces native SaveData without erasing allocated prestige");
for(var identity=405;identity<1405;identity++){
 scoped.BindCurrent(identity);
 Check(scoped.Ranks.SequenceEqual(completed),"Repeated native save replacements preserve the active save's ranks");
}
scoped.Restore(completed,true);scoped.BindLoaded(505);scoped.Ranks[0]=0;
Check(completed[0]==20,"Runtime purchases/respec cannot mutate the loaded input snapshot");
Console.WriteLine("ALL SAVE OWNERSHIP CHECKS PASSED");

for(var state=0;state<=13;state++){
 Check(!DeliveryPauseRules.ShouldWait(false,state,2),"Enabled deliveries never block the native iterator");
 if(state is >0 and <13)Check(!DeliveryPauseRules.ShouldWait(true,state,2),"Pausing cannot interrupt travel or unloading");
}
Check(!DeliveryPauseRules.ShouldWait(true,0,0),"A paused new day still starts its first native delivery");
Check(!DeliveryPauseRules.ShouldWait(true,13,.99f),"The returning truck can finish its movement");
Check(DeliveryPauseRules.ShouldWait(true,13,1)&&DeliveryPauseRules.ShouldWait(true,13,1.1f),"Completed return holds the next visit despite frame timing overshoot");
var deliveryCount=1;var deliveryState=13;
for(var frame=0;frame<1000;frame++){
 if(!DeliveryPauseRules.ShouldWait(true,deliveryState,1)){deliveryCount++;deliveryState=0;}
 Check(deliveryCount==1&&deliveryState==13,"Paused frames cannot start or reset another truck visit");
}
Check(!DeliveryPauseRules.ShouldWait(false,deliveryState,1),"Resume releases the same native iterator");
for(var day=1;day<=100;day++){
 Check(!DeliveryPauseRules.ShouldWait(true,0,0),"Each day guarantees its first truck while keeping the pause setting");
 for(var state=1;state<=12;state++)Check(!DeliveryPauseRules.ShouldWait(true,state,.5f),"The first daily truck can travel, unload and return");
 Check(DeliveryPauseRules.ShouldWait(true,13,1),"After the first completed visit all additional deliveries remain paused");
 Check(!DeliveryPauseRules.ShouldWait(false,13,1),"The operator can allow extra deliveries on any day");
}
Console.WriteLine("ALL DELIVERY PAUSE CHECKS PASSED; button rendering, input and native truck transitions require manual gameplay validation.");
Check(IntakeRimRules.Supported(.057f,-1.97f,.11263f),"Audited small raised crusher rim is eligible for a flush collision surface");
Check(!IntakeRimRules.Supported(.057f,-1.97f,.4f),"A large machine obstacle must not be flattened");
Check(!IntakeRimRules.Supported(0,-2,float.NaN)&&!IntakeRimRules.Supported(0,float.NegativeInfinity,.05f),"Unknown or invalid rim geometry is rejected");
Check(IntakeRimRules.FlushHeight(.11263f,.057f)==.057f,"The raised lip becomes flush with the walking floor");
Check(IntakeRimRules.FlushHeight(-1.97f,.057f)==-1.97f&&IntakeRimRules.FlushHeight(-.3f,.057f)==-.3f,"Lower pit collision walls retain their original heights");
Check(IntakeRimRules.FlushHeight(.057f,.057f)==.057f,"Existing floor-level seams stay unchanged");
Console.WriteLine("ALL FLUSH INTAKE RIM CHECKS PASSED; body passage over the seam still requires manual physics validation.");
