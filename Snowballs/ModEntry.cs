using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using StardewValley.Projectiles;
using HarmonyLib;
using System;
using StardewValley.Monsters;
using StardewValley.Locations;

namespace Snowballs
{
    public sealed class ModConfig
    {
        public int HeartLimit { get; set; } = 3;
        public int FriendshipBonus { get; set; } = 10;
        public int StunTime { get; set; } = 3;
    }
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private const string snowballName = "(O)Typhe.SnowballAssets_Snowball";
        private static ModEntry instance;
        public override void Entry(IModHelper helper)
        {
            instance = this;
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.World.TerrainFeatureListChanged += HoeSnoe; // dr seuss lol
            var harmony = new Harmony("Typhe.Snowballs");
            try
            {
                var collisionMethod = AccessTools.Method(typeof(Projectile), "behaviorOnCollision");
                harmony.Patch(original: collisionMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(CollisionFix)));
                Monitor.Log("Harmony patches applied.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Harmony patching failed: {ex.Message}", LogLevel.Error);
            }
        }

        private static bool IsSnowball(BasicProjectile proj)
        {
            // Not sure if any other projectiles do this little damage, but using -1 damage causes a weird bugged damage number above monsters
            return proj.projectileID.Value == -1 && proj.damageToFarmer.Value <= 1;
        }
        public static bool CollisionFix(Projectile __instance, GameLocation location, Character target, TerrainFeature terrainFeature)
        {
            if (__instance is BasicProjectile basicProj)
            {
                if (!IsSnowball(basicProj))   
                    return true;
                if (target != null)
                {
                    Game1.playSound("snowyStep");
                    location.temporarySprites.Add(new TemporaryAnimatedSprite(44, target.Position, Color.White));
                    if (target is not Monster && target is NPC npcTarget)
                    {
                        int currentFriendship = 0;
                        if (Game1.player.friendshipData.TryGetValue(npcTarget.Name, out Friendship friendship))
                        {
                            currentFriendship = friendship.Points;
                        }
                        
                        if (currentFriendship > instance.Config.HeartLimit * 250 - 1)
                        {
                            if (Game1.random.NextDouble() < 0.10)
                            {
                                friendship.Points += instance.Config.FriendshipBonus;
                                npcTarget.doEmote(56); // music note emote :)
                            }
                            else
                            {
                                npcTarget.doEmote(20); // heart emote <3
                            }
                        }
                        else
                        {
                            npcTarget.doEmote(16); // exlamation point emote :o
                        }
                    }
                    if (target is Monster monsterTarget)
                    {
                        monsterTarget.Halt();
                        monsterTarget.stunTime.Value = instance.Config.StunTime * 1000;
                    }
                    if (location.projectiles.Contains(__instance))
                    {
                        location.projectiles.Remove(__instance);
                    }
                    return false;
                }
            }
            // non-snowballs run normally
            return true;
        }

        private void HoeSnoe(object sender, TerrainFeatureListChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            var Location = e.Location;
            bool isSnowyMines = false;
            bool validLocation = false;
            if (Location is MineShaft mine && mine.mineLevel >= 40 && mine.mineLevel < 80)
                isSnowyMines = true;
            if (Location.IsOutdoors && !Location.InIslandContext() && !Location.InDesertContext() || isSnowyMines)
                validLocation = true;
            if (validLocation == false || (Game1.currentSeason != "winter" && !isSnowyMines))
                return;
            foreach (var pair in e.Added)
            {
                if (pair.Value is HoeDirt)
                {
                    Vector2 tile = pair.Key;
                    Item snowball = ItemRegistry.Create(snowballName, 1, allowNull: false);
                    Game1.createItemDebris(snowball, new Vector2(tile.X * 64, tile.Y * 64), -1);
                }
            }
        }
    }
}

