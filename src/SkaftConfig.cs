using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace Skaft
{
    /// <summary>
    /// Everything tunable, bound in one place so the .cfg reads as a document rather than as
    /// whatever order the code happened to need things in.
    ///
    /// The standing BepInEx trap applies: every entry is written to disk on first run and the
    /// saved value beats a new default in code. Changing a default here does nothing on a
    /// machine that has already run the plugin - edit
    /// <c>&lt;profile&gt;\BepInEx\config\ezomic.valheim.skaft.cfg</c> as part of the same
    /// change. When a config-driven change appears to do nothing in game, read the cfg before
    /// reading any code.
    /// </summary>
    internal static class SkaftConfig
    {
        public static ConfigEntry<bool> Enabled;

        public static ConfigEntry<float> MinRadius;
        public static ConfigEntry<float> MaxRadius;
        public static ConfigEntry<float> FullLevel;
        public static ConfigEntry<float> Curve;

        public static ConfigEntry<float> CostMultiplier;
        public static ConfigEntry<float> DurabilityFloor;
        public static ConfigEntry<int> MaxPieces;

        public static ConfigEntry<bool> RepairItems;
        public static ConfigEntry<int> MinItems;
        public static ConfigEntry<int> MaxItems;
        public static ConfigEntry<float> ItemCurve;

        public static ConfigEntry<bool> OwnBuildingsOnly;
        public static ConfigEntry<bool> ShowReachInBuildMenu;
        public static ConfigEntry<string> ReachEntries;

        public static ConfigEntry<bool> Verbose;

        private static string _reachRaw;
        private static HashSet<string> _reachSet;

        /// <summary>
        /// Whether the build menu entry with this prefab name is one the sweep can actually
        /// serve, and so one the reach line belongs on. Parsed on demand and re-parsed only when
        /// the string changes, because a host push or the config manager can rewrite it live.
        /// </summary>
        internal static bool IsReachEntry(string prefabName)
        {
            string raw = ReachEntries.Value ?? string.Empty;

            if (raw != _reachRaw || _reachSet == null)
            {
                _reachRaw = raw;
                _reachSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string part in raw.Split(','))
                {
                    string trimmed = part.Trim();
                    if (trimmed.Length > 0) _reachSet.Add(trimmed);
                }
            }

            return _reachSet.Contains(prefabName);
        }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("Skaft", "Enabled", true,
                "The whole mod, both halves of it: the hammer sweep on buildings and the Repair "
                + "button at the bench. Off leaves vanilla single-piece repair untouched in both "
                + "places - which is also exactly what this mod does at Crafting 0, so turning it "
                + "off is only useful for telling the mod apart from a low skill level. To keep "
                + "one half and drop the other, use RepairItems.");

            MinRadius = config.Bind("Skaft", "MinRadius", 0f,
                "Sweep radius in metres at Crafting 0, measured from the piece under your "
                + "cursor. Zero on purpose: a new character gets vanilla repair, one piece per "
                + "swing, and the mod is invisible until the skill is worth something. At zero "
                + "the sweep does not run at all, so a new character cannot meet a bug in it. "
                + "Raise this only if you want the reach handed over rather than earned.");

            MaxRadius = config.Bind("Skaft", "MaxRadius", 8f,
                "Sweep radius in metres once Crafting reaches FullLevel. Vanilla lets you "
                + "touch a piece from 5 metres away, so 8 metres around what you are already "
                + "touching is the outer edge of \"working on this building\". At 8 a mid-wall "
                + "swing covers a 10x6 longhouse end to end. Larger numbers repair things you "
                + "cannot see, and your stamina stops paying for them long before the radius "
                + "runs out.");

            FullLevel = config.Bind("Skaft", "FullLevel", 60f,
                "The Crafting level at which the radius reaches MaxRadius and a bench press "
                + "reaches MaxItems. Shared by both halves on purpose: one skill, one level at "
                + "which it has arrived. Above it nothing "
                + "changes. 100 is the wrong number to design against: it costs roughly 20,300 "
                + "crafts, and Crafting only rises by crafting or upgrading at a station and by "
                + "repairing a worn item - repairing buildings trains nothing at all. 60 is "
                + "about 5,700, which a long playthrough actually reaches, so the reward "
                + "arrives instead of dangling.");

            Curve = config.Bind("Skaft", "Curve", 0.8f,
                "Exponent on the skill fraction before it is lerped between MinRadius and "
                + "MaxRadius: radius = min + (max-min) * (level/FullLevel)^Curve. 1.0 is a "
                + "straight line. Below 1 opens the reach earlier, above 1 hoards it for the "
                + "top levels. The default keeps the first ten levels indistinguishable from "
                + "vanilla - 1.9m cannot reach a neighbour two metres away - then grows "
                + "steadily, because the experience curve is already brutal at the top and "
                + "stacking a second brake on it puts the whole mod somewhere nobody goes.");

            CostMultiplier = config.Bind("Skaft", "CostMultiplier", 1f,
                "Multiplies the stamina, eitr and hammer durability charged for each piece the "
                + "sweep actually repairs. 1 means every piece costs exactly what vanilla "
                + "charges to repair one piece by hand. That per-piece price is what stops a "
                + "wide radius being free: your stamina bar, not the radius, decides how many "
                + "pieces a swing fixes. Above 1 makes a sweep dearer than doing it by hand. "
                + "Below 1 is the setting that deletes the point of this mod. Pieces already at "
                + "full health cost nothing either way.");

            DurabilityFloor = config.Bind("Skaft", "DurabilityFloor", 1f,
                "The sweep stops before the hammer would drop to or below this. Repairing "
                + "subtracts durability without checking zero, so a wide sweep can spend an "
                + "entire hammer inside one click - the game tells you it broke, but it broke on "
                + "a single press rather than over the swings that wore it down, and it "
                + "unequips, which drops you out of build mode mid-job. Zero lets a sweep spend "
                + "the hammer to its last point and break it on the next swing, the normal way.");

            MaxPieces = config.Bind("Skaft", "MaxPieces", 200,
                "Hard ceiling on pieces repaired in one swing, whatever the radius says. This "
                + "is a network guard, not a balance number - each repaired piece is one "
                + "message to the piece's owner and one broadcast back, so 200 is 400 messages "
                + "in a single frame. Balance is stamina and durability.");

            RepairItems = config.Bind("Skaft", "RepairItems", true,
                "Whether one press of the Repair button at a crafting station repairs more than "
                + "one item. Off leaves the bench exactly as vanilla has it, one item a press, "
                + "and the hammer sweep is unaffected either way. This is the only switch that "
                + "separates the two halves of the mod.");

            MinItems = config.Bind("Skaft", "MinItems", 1,
                "How many items one press of the Repair button fixes at Crafting 0. One is "
                + "vanilla, and it is the floor for the same reason MinRadius is zero: a new "
                + "character should get the game as it ships, and the mod should be something "
                + "the character grows into rather than something the install hands over.");

            MaxItems = config.Bind("Skaft", "MaxItems", 10,
                "How many items one press of the Repair button fixes once Crafting reaches "
                + "FullLevel. Ten is a full kit - helmet, chest, legs, cape, weapon, shield, bow "
                + "and the three tools - so at the top of the curve coming home is one press "
                + "rather than ten. Nothing is made cheaper by this: vanilla charges no "
                + "materials, no durability and no stamina to repair an item, so the presses "
                + "were the whole price and the skill is what buys them down. Unlike the sweep "
                + "there is no stamina bar to stop it, which is why the number itself is the "
                + "constraint and why it starts at one.");

            ItemCurve = config.Bind("Skaft", "ItemCurve", 1.5f,
                "Exponent on the skill fraction for the bench, the way Curve is for the radius: "
                + "items = min + (max-min) * (level/FullLevel)^ItemCurve, floored. It is a "
                + "separate number, and above 1 where Curve is below it, because a count is a "
                + "much coarser dial than metres - one more metre of reach is nothing, one more "
                + "item is the difference between pressing twice and pressing once. At the "
                + "default the bench is vanilla until about Crafting 14, three items around 25, "
                + "seven around 50 and ten at 60.");

            OwnBuildingsOnly = config.Bind("Skaft", "OwnBuildingsOnly", false,
                "Restrict the sweep to pieces you placed yourself. False matches vanilla, which "
                + "happily repairs anyone's building and leaves permission entirely to wards. "
                + "True is for shared bases where you would rather not top up a neighbour's "
                + "walls with your own stamina. The piece under your cursor is always vanilla's "
                + "business, not this setting's.");

            ShowReachInBuildMenu = config.Bind("Skaft", "ShowReachInBuildMenu", true,
                "Add a line to the Repair entry in the build menu showing your current reach in "
                + "metres and the Crafting level it came from. This is the only place the "
                + "number is shown; after a swing, the \"Repaired x12\" count tells you the "
                + "rest.");

            ReachEntries = config.Bind("Skaft", "ReachEntries", "piece_repair",
                "Comma separated prefab names of the build menu entries the reach line is "
                + "written on. It is not simply \"whatever is flagged as a repair entry\", "
                + "because that flag means \"this entry clicks on the world instead of placing "
                + "into it\" and other mods use it for their own tools - Vaettir's Transplant "
                + "entry on the cultivator wears it, and the sweep can never run there. Add a "
                + "name here if some other mod's repair entry does fall through to the game's "
                + "own repair; the sweep itself is unaffected either way.");

            // Not synced by intent - see the plugin. A diagnostic flag is personal, and a host
            // turning on someone else's logging is not a thing anybody asked for.
            Verbose = config.Bind("Diagnostics", "Verbose", false,
                "One line per swing to BepInEx/LogOutput.log: skill level, radius, candidates "
                + "found, pieces repaired, and what stopped the sweep. For tuning the curve, "
                + "noisy in normal play.");
        }
    }
}
