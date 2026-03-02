using StardewModdingAPI;
using StardewValley;
namespace MinecartIssues;

public class BreakdownManager
{
    private readonly IMonitor _logger;
    private readonly ModConfig _config;
    private readonly ModData _cartData;

    // letter names from the content pack
    public const string Tunnel = "CollapsedTunnel";
    public const string Rockslide = "Rockslide"; // very creative name i know thanks
    public const string Supports = "RottenSupports";
    public const string Tree = "TreeOnTracks";
    public const string Axle = "AxleSnapped";
    public const string Cow = "CowOnTracks"; // yes moving a cow takes a few days
    public const string Sentience = "SadMinecart";
    public const string FuckYouBill = "BillBrokeIt"; //stanley romancable npc when???

    private static readonly string[] BreakdownTypes = 
    { 
        Tunnel,
        Rockslide,
        Supports,
        Tree,
        Axle,
        Cow,
        Sentience,
        FuckYouBill
    };

    public BreakdownManager(IMonitor monitor, ModData cartData, ModConfig config)
    {
        _logger = monitor;
        _cartData = cartData;
        _config = config;
    }
    public void OnDayStarted()
    {
        CheckBreakPlan();
        PlanBreak();
        CheckRepair(GetAbsoluteDay());
    }

    private void PlanBreak()
    {
        if (_cartData.MinecartBroken)
        {
            return;
        }
        if (_config.QuickRepairCost <= 0)
        {
            _logger.Log("Repair cost is set to 0 or less (probably won't break anything)", LogLevel.Info);
        }
        if (_config.RepairTimeDays <= 0)
        {
            _logger.Log("Repair time is set to 0 or less, skipping break plan (why did you even download this mod?)", LogLevel.Info);
            return;
        }
        if (_config.DailyBreakChance <= 0 || _config.DailyBreakChance > 100)
        {
            _logger.Log("Daily break chance isn't between 0 and 100, skipping break plan", LogLevel.Info);
            return;
        }
        if (Game1.random.NextDouble() < _config.DailyBreakChance / 100f)
        {
            _cartData.PlanBreak = true;
        }
    }

    private void CheckBreakPlan()
    {
        if (_cartData.PlanBreak)
        {
            int typeIndex = Game1.random.Next(BreakdownTypes.Length);
            string breakType = BreakdownTypes[typeIndex];
            _cartData.MinecartBroken = true;
            _cartData.BreakType = breakType;
            _cartData.PlanBreak = false;
            _cartData.BreakAbsoluteDay = GetAbsoluteDay();
        }
    }
    // vague corporate entity repairs the minecart after (config) days
    // not adding randomness idea since players set repair time in config
    private void CheckRepair(int currentAbsoluteDay)
    {
        if (!_cartData.MinecartBroken)
        {
            return;
        }

        if (currentAbsoluteDay - _cartData.BreakAbsoluteDay >= _config.RepairTimeDays)
        {
            RepairMinecart();
        }
    }
    private void RepairMinecart()
    {
        _cartData.MinecartBroken = false;
        _cartData.BreakType = string.Empty;
        Game1.addHUDMessage(new HUDMessage($"The minecart system has been repaired.",HUDMessage.newQuest_type));
        // exclamation point from quests
    }

    public static int GetAbsoluteDay()
    {
        int seasonIndex = GetSeasonIndex(Game1.currentSeason);
        return ((Game1.year - 1) * 112) + (seasonIndex * 28) + (Game1.dayOfMonth - 1);
    }
    private static int GetSeasonIndex(string season)
    {
        return season switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };
    }
}
