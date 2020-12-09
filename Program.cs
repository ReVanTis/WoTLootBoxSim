using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LookBoxSim
{
    [Serializable]
    public struct LootBoxItem
    {
        public string Name {get; set;}
        //Remove from prize pool if already owned, otherwise do refund logic.
        public bool NoDupe {get; set;}
        //Refund how much gold if dupe
        public int Refund {get; set;}
        public int Amount{get; set;}
        public bool Owned {get; set;}
    }
    
    [Serializable]
    public struct LootBoxItemCatogory
    {
        public string Name {get; set;}
        // Rate = 0 means fixed drop
        public double Rate {get; set;}
        public LootBoxItem[] Items {get; set;}
        // 0 = disable, otherwise if this category is not draw in a row until reached counter limit
        // next draw will draw this cat 100%.
        public int UseCounter {get; set; }
    }

    [Serializable]
    public class LootBox
    {
        public LootBoxItemCatogory[] Cats {get; set;}
        
        public List<LootBoxItem> Draw()
        {
            Random r = new Random();
            List<LootBoxItem> items = new List<LootBoxItem>();
            double rates = 0;
            foreach(var c in Cats)
            {
                // Your luck number!
                double luck = r.NextDouble();
                if(c.Rate == 0)
                //Rate = 0 means it's fixed drop
                {
                    var DrawItems = c.Items.Where(i=> (Program.OwnedStatus[i.Name] == false) || (Program.OwnedStatus[i.Name] == true && i.NoDupe == false)).ToList();
                    int index = r.Next(DrawItems.Count);
                    items.Add(DrawItems[index]);
                    Program.OwnedStatus[DrawItems[index].Name]=true;
                }
                else
                {
                    //this one is drawed.
                    if (rates < luck && luck < rates + c.Rate)
                    {
                        // Prize pool criteria: (not owned) OR (own and allow duplicates).
                        var DrawItems = c.Items.Where(i=> (Program.OwnedStatus[i.Name] == false) || (Program.OwnedStatus[i.Name] == true && i.NoDupe == false)).ToList();
                        int index = r.Next(DrawItems.Count);
                        // If owned and dupe, do refund gold.
                        if(Program.OwnedStatus[DrawItems[index].Name] && DrawItems[index].Refund!=0)
                        {
                            LootBoxItem RefundGold = new LootBoxItem();
                            RefundGold.Name="Gold Refund";
                            RefundGold.Amount=DrawItems[index].Refund;
                            items.Add(RefundGold);
                        }
                        else
                        {
                            items.Add(DrawItems[index]);
                            Program.OwnedStatus[DrawItems[index].Name]=true;
                        }
                        // clear counter if this cat is drawed.
                        if(c.UseCounter != 0)
                        {
                            if(Program.LootBoxItemCatCounter.ContainsKey(c.Name))
                            {
                                Program.LootBoxItemCatCounter[c.Name] = 0;
                            }
                            else
                            {
                                Program.LootBoxItemCatCounter.Add(c.Name, 0);
                            }
                        }
                    }
                    // no luck...
                    else
                    {
                        //Check if counter logic should be used.
                        if(c.UseCounter != 0)
                        {
                            if(!Program.LootBoxItemCatCounter.ContainsKey(c.Name))
                            {
                                Program.LootBoxItemCatCounter.Add(c.Name, 0);
                            }
                            Program.LootBoxItemCatCounter[c.Name] = Program.LootBoxItemCatCounter[c.Name] + 1;              
                            if(Program.LootBoxItemCatCounter[c.Name] == c.UseCounter)
                            {
                                 //this one is drawed.
                                var DrawItems = c.Items.Where(i=> (Program.OwnedStatus[i.Name] == false) || (Program.OwnedStatus[i.Name] == true && i.NoDupe == false)).ToList();
                                int index = r.Next(DrawItems.Count);
                                if(Program.OwnedStatus[DrawItems[index].Name] && DrawItems[index].Refund!=0)
                                {
                                    LootBoxItem RefundGold = new LootBoxItem();
                                    RefundGold.Name="Gold Refund";
                                    RefundGold.Amount=DrawItems[index].Refund;
                                    items.Add(RefundGold);
                                }
                                else
                                {
                                    items.Add(DrawItems[index]);
                                    Program.OwnedStatus[DrawItems[index].Name]=true;
                                }
                                Program.LootBoxItemCatCounter[c.Name] = 0;
                            }
                        }
                    }
                    rates = rates + c.Rate;
                }
            }
            return items;
        }
    }

    public class Program
    {
        public static Dictionary<string,int> LootBoxItemCatCounter = new Dictionary<string,int>();
        public static Dictionary<string,bool> OwnedStatus = new Dictionary<string, bool>();
        public static Dictionary<string,int> summary = new Dictionary<string, int>();
        public static LootBox box;
        public static int verbosity = 0;
        public static void init()
        {
            LootBoxItemCatCounter = new Dictionary<string, int>();
            OwnedStatus = new Dictionary<string, bool>();
            summary = new Dictionary<string, int>();
            foreach(var cat in box.Cats)
            {
                foreach(var item in cat.Items)
                {
                    if(OwnedStatus.ContainsKey(item.Name))
                    {
                        OwnedStatus[item.Name]=item.Owned;
                    }
                    else
                    {
                        OwnedStatus.Add(item.Name, item.Owned);
                    }
                    if(summary.ContainsKey(item.Name))
                    {
                        summary[item.Name]=0;
                    }
                    else
                    {
                        summary.Add(item.Name, 0);
                    }
                }
            }
            summary.Add("Gold Refund", 0);
        }
        static void Log(string output, int v=0)
        {
            if(verbosity >= v)
                Console.Write(output);
        }
        static void LogLine(string output, int v=0)
        {
            if(verbosity >= v)
                Console.WriteLine(output);
        }
        static void Main(string[] args)
        {
            string LootBoxJson = File.ReadAllText("2020_LootBox.json");
            box = JsonSerializer.Deserialize<LootBox>(LootBoxJson);
            int roundTotal = 100000;
            int boxPerRound = 100;
            
            if(args.Length >= 1)
            {
                if(!int.TryParse(args[0], out boxPerRound))
                {
                    Console.WriteLine("Input invalid, use 50 as box per round");
                    boxPerRound=50;
                }
            }
            if(args.Length >= 2)
            {
                if(!int.TryParse(args[1], out roundTotal))
                {
                    Console.WriteLine("Input invalid, use 100000 as total round");
                    roundTotal = 100000;
                }
            }
            if(args.Length >= 3)
            {
                if(!int.TryParse(args[2], out verbosity))
                {
                    Console.WriteLine("Input invalid, use 0 as verbosity");
                    verbosity = 0;
                }
            }

            double GoldTotal =0;
            double SilverTotal=0;
            double VIPTotal =0;
            Dictionary<int,int> T8Stats = new Dictionary<int,int>(){{0,0},{1,0},{2,0},{3,0},{4,0}};
            for(int round = 0; round < roundTotal ; round++)
            {
                LogLine($"Round {round+1}/{roundTotal}", 1);
                init();
                int count = 0;
                int T8Count = 0;
                List<LootBoxItem> items = new List<LootBoxItem>();
                for(int i =0 ; i < boxPerRound ; i++)
                {
                    count ++;
                    var ItemsInBox = box.Draw();
                    Log($"No.{count}, you get:",2);
                    foreach(var item in ItemsInBox)
                    {
                        Log($"{item.Name} * {item.Amount}, ",2);
                        if(item.Name.Contains("[8]"))
                        {
                            T8Count++;
                        }
                        items.Add(item);
                    }
                    
                    LogLine("", 2);
                    Log($"Counters: ",2);
                    foreach(var kv in LootBoxItemCatCounter)
                    {
                        Log($"{kv.Key}={kv.Value}, ",2);
                    }
                    LogLine("",2);
                }
                foreach(var i in items)
                {
                    if(!summary.ContainsKey(i.Name))
                    {
                        summary.Add(i.Name,i.Amount);
                    }
                    else
                    {
                        summary[i.Name] = summary[i.Name] + i.Amount;
                    }
                }
                if(verbosity >= 1)
                {
                    Console.WriteLine($"Total box {count}, summary:");
                    var sList = summary.Keys.ToList();
                    sList.Sort();
                    foreach(var k in sList)
                    {
                        if(summary[k] != 0)
                            Console.WriteLine($"{k} * {summary[k]}, ");
                    }
                    Console.WriteLine("Stats:");
                    Console.WriteLine($"Silver /box: {(double)summary["Silver"] / (double)count * 10000d}");
                    Console.WriteLine($"Gold   /box: {((double)summary["Gold"] + (double) summary["Gold Refund"]) / (double)count}");
                    Console.WriteLine($"VIP    /box: {(double)summary["VIP"] / (double)count}");
                    Console.WriteLine();
                }
                SilverTotal += summary["Silver"];
                GoldTotal += summary["Gold"] + summary["Gold Refund"];
                VIPTotal+= summary["VIP"];
                T8Stats[T8Count] = T8Stats[T8Count]+1;
            }
            Console.WriteLine("Final Stats:");
            Console.WriteLine($"Box per round: {boxPerRound}, Total rounds: {roundTotal}.");
            for(int i=0; i<=4; i++)
            {
                Console.WriteLine($"T8 * {i}: {(double)T8Stats[i]/(double)roundTotal:P}");
            }
            Console.WriteLine($"Silver /box: {((double)SilverTotal / (double) (roundTotal * boxPerRound)) * 10000d}");
            Console.WriteLine($"Gold   /box: {(double)GoldTotal / (double)(roundTotal * boxPerRound)}");
            Console.WriteLine($"VIP    /box: {(double)VIPTotal / (double)(roundTotal*boxPerRound)}");
        }
    }
}
