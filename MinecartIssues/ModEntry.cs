using System;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;
using System.Collections.Generic;

namespace MinecartIssues;
public class ModEntry : Mod
{
    private ModData _modData = new();
    private ModConfig _config = null!;
    private BreakdownManager _breakdownManager = null!;
    private bool PopupShownToday;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _breakdownManager = new BreakdownManager(this.Monitor, _modData, _config);
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStart;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.GameLoop.Saving += OnSaving;
    }
    // I have no clue where i got this from, but it seems to work
    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    {
        if (!Game1.IsMultiplayer && Context.IsWorldReady)
        {
            var saveData = this.Helper.Data.ReadSaveData<ModData>("MinecartIssues.SaveData");
            if (saveData != null)
            {
                _modData = saveData;
                _breakdownManager = new BreakdownManager(this.Monitor, _modData, _config);
            }
        }
    }

    private void OnDayStart(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
    {
        PopupShownToday = false;
        _modData.LetterShown = false;
        _breakdownManager.OnDayStarted();
        this.Helper.Data.WriteSaveData("MinecartIssues.SaveData", _modData);        
    }

    private void OnButtonPressed(object? sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
    {
        if (e.Button != SButton.MouseRight)
        {
            return;
        }

        // mouse pos
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();

        // convert to tile
        int tileX = (Game1.viewport.X + mouseX) / Game1.tileSize;
        int tileY = (Game1.viewport.Y + mouseY) / Game1.tileSize;

        if (IsMinecart(tileX, tileY))
        {
            if (_modData.MinecartBroken)
            {
                this.Helper.Input.Suppress(SButton.MouseRight); // dont open gui
                MinecartClicked();
            }
        }
    }

    private void MinecartClicked()
    {
        int daysBroken = BreakdownManager.GetAbsoluteDay() - _modData.BreakAbsoluteDay;
        if (!_modData.LetterShown)
        {
            _modData.LetterShown = true;
            string key = $"Minecart.{_modData.BreakType}";
            string letterText = Game1.content.Load<Dictionary<string, string>>("Data/mail")[key];
            letterText = letterText.Replace("@", Game1.player.Name);
            Game1.activeClickableMenu = new StardewValley.Menus.LetterViewerMenu(letterText);
        }
        else if (!PopupShownToday && (daysBroken < _config.RepairTimeDays - 1))
        {
            PopupShownToday = true;
            // yes/no popup to pay for repair
            Game1.currentLocation.createQuestionDialogue(
                $"The minecart tracks are broken. Pay {_config.QuickRepairCost}g to bribe the company to fix it faster?",
                new Response[]
                {
                    new Response("Yes", "Yes"),
                    new Response("No", "No")
                },
                (Farmer who, string answer) =>
                {
                    if (answer == "Yes" && who.Money >= _config.QuickRepairCost)
                    {
                        who.Money -= _config.QuickRepairCost;
                        _modData.BreakAbsoluteDay = BreakdownManager.GetAbsoluteDay() - _config.RepairTimeDays; // trigger repair on next day start
                        Game1.addHUDMessage(new HUDMessage("Minecart repair scheduled.", HUDMessage.newQuest_type));
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage("Minecart remains broken.", HUDMessage.error_type));
                    }
                }
            );
        }
        // If seen popup, show reminder
        else if (daysBroken < _config.RepairTimeDays - 1)
        {
        Game1.addHUDMessage(new HUDMessage("The minecart system is currently out of service.",HUDMessage.error_type));                    
        }
        // alternative reminder for final day
        else if (daysBroken == _config.RepairTimeDays - 1)
        {
            Game1.addHUDMessage(new HUDMessage("The minecart system will be operational tomorrow.", HUDMessage.error_type));
        }
    }

    private bool IsMinecart(int x, int y)
    {
        if (IsMinecartAt(x, y))
        {
            return true;
        }

        //minecart hitbox is sometimes 2 tiles tall??
        if (IsMinecartAt(x, y + 1))
        {
            return true;
        }
        return false;
    }

    private bool IsMinecartAt(int x, int y)
    {
        var tile = Game1.currentLocation?.map?.GetLayer("Buildings")?.PickTile(new Location(x * Game1.tileSize, y * Game1.tileSize), Game1.viewport.Size);
        if (tile != null && tile.Properties.ContainsKey("Action"))
        {
            string? action = tile.Properties["Action"]?.ToString();
            if (action != null && action.IndexOf("Minecart", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        // check for minecart
        if (Game1.currentLocation?.Objects != null)
        {
            var tilePos = new Microsoft.Xna.Framework.Vector2(x, y);
            if (Game1.currentLocation.Objects.TryGetValue(tilePos, out var obj) && obj.Name == "Minecart")
            {
                return true;
            }
        }
        return false;
    }

    private void OnSaving(object? sender, StardewModdingAPI.Events.SavingEventArgs e)
    {
        if (!Game1.IsMultiplayer) // gonna pretend multiplayer doesnt exist for now
        {
            this.Helper.Data.WriteSaveData("MinecartIssues.SaveData", _modData);
        }
    }
}
