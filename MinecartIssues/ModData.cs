namespace MinecartIssues;
public class ModData
{
    public bool MinecartBroken { get; set; }
    // CollapsedTunnel, Rockslide, RottenSupports, TreeOnTracks, AxleSnapped, CowOnTracks, BillBrokeIt
    public string BreakType { get; set; } = string.Empty;
    // absolute day is days since year 1 day 1, simpler than storing year and season
    public int BreakAbsoluteDay { get; set; }
    public bool LetterShown { get; set; }

    // calculating the day before prevents savescumming
    public bool PlanBreak { get; set; }
}
public sealed class ModConfig
{
    public int DailyBreakChance { get; set; } = 1;
    public int RepairTimeDays { get; set; } = 3;
    public int QuickRepairCost { get; set; } = 5000;
}
