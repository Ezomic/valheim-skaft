using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Skaft
{
    /// <summary>
    /// The other half of repair: the button at the workbench.
    ///
    /// Skaft's sweep covers buildings, and buildings are only half of what this game asks you to
    /// repair. The other half is the Repair button on the crafting panel, which fixes exactly one
    /// item per press - so coming home from the swamp means standing at the bench pressing the
    /// same button eight times to clear a kit. That is the same tedium the sweep was written for,
    /// reached through a different verb, and it is answered the same way: the Crafting skill buys
    /// how many items one press fixes, and nothing else changes.
    ///
    /// Nothing here is a discount, and that is not an argument this file has to make - vanilla
    /// charges nothing at all to repair an item. Materials are not spent, durability is not
    /// spent, and the only price is the clicking. So the constraint cannot be the price, and it
    /// is the count instead: at Crafting 0 a press repairs one item, exactly as it always did,
    /// and the number only reaches <see cref="SkaftConfig.MaxItems"/> at the same FullLevel the
    /// radius does.
    ///
    /// The skill this grants is vanilla's, unchanged and per item. RepairOneItem calls
    /// RaiseSkill(Crafting, 1 - durability/max) for the item it fixes, so repairing nine items in
    /// one press raises Crafting by exactly what nine presses raised it by. The loop that would
    /// have been a problem - Crafting buying faster Crafting - is therefore not one: the skill
    /// paid out per item is identical, and all that changes is how many times you press a button
    /// to collect it.
    ///
    /// Vanilla's own gate is the authority for what may be repaired. CanRepair is private, and it
    /// carries the recipe lookup, the repair-station match, the station level and the world level
    /// - four rules that a copy here would own forever and get wrong after one update. It is
    /// called through a bound delegate instead, so this file decides how many items and vanilla
    /// decides which.
    /// </summary>
    internal static class Bench
    {
        /// <summary>
        /// Reused between presses. GetWornItems appends rather than replacing, so this is cleared
        /// on both sides of the loop; holding item data between presses would be holding
        /// references to stacks that may since have been dropped, split or eaten.
        /// </summary>
        private static readonly List<ItemDrop.ItemData> Worn = new List<ItemDrop.ItemData>();

        /// <summary>
        /// Bound lazily, never in a static initializer, for the reason spelled out in
        /// <see cref="Sweep"/>: a binding that throws at type-init poisons every Harmony patch
        /// the class carries, and that presents as unrelated vanilla features breaking rather
        /// than as this feature failing.
        /// </summary>
        private static Func<InventoryGui, ItemDrop.ItemData, bool> _canRepair;

        private static bool _bound;
        private static bool _bindFailed;

        /// <summary>
        /// How many items one press of the Repair button fixes for this player right now.
        ///
        /// Same shape as the radius and the same FullLevel, because it is the same idea about the
        /// same skill - but its own exponent, because a count is a far coarser dial than metres.
        /// One extra metre of reach is nothing; one extra item is the difference between pressing
        /// twice and pressing once. Sharing <see cref="SkaftConfig.Curve"/> at 0.8 would hand out
        /// three items a press at Crafting 10, which is most of the feature, at a level where the
        /// radius is deliberately still worth nothing.
        /// </summary>
        internal static int Count(Player player)
        {
            float level = player.GetSkillFactor(Skills.SkillType.Crafting) * 100f;

            float full = Mathf.Max(1f, SkaftConfig.FullLevel.Value);
            float t = Mathf.Pow(Mathf.Clamp01(level / full), Mathf.Max(0.01f, SkaftConfig.ItemCurve.Value));

            int min = Mathf.Max(1, SkaftConfig.MinItems.Value);
            int max = Mathf.Max(min, SkaftConfig.MaxItems.Value);

            // Floor, not round. Rounding would hand out the next item half a level early, and the
            // number is small enough that each step is a visible promotion - it should be earned
            // outright rather than by rounding up.
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max, t)), min, max);
        }

        /// <summary>
        /// Repair the rest of this press's allowance, after vanilla has repaired the first item.
        /// </summary>
        internal static void Run(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            if (player == null || gui == null) return;

            // Minus one: vanilla's RepairOneItem already did the first, which is what this is a
            // postfix of. At Count 1 there is no allowance left and the feature is invisible,
            // which is exactly what a character below the curve should get.
            int budget = Count(player) - 1;
            if (budget <= 0) return;

            if (!Bind()) return;

            // RepairOneItem's own two early returns, mirrored rather than inherited.
            //
            // They have to be here because CanRepair does not carry them: it asks whether the
            // station can repair this item, not whether the station may be used at all. Vanilla
            // runs CheckUsable once at the top of the press and then trusts it. Without these two
            // lines a press at a station vanilla refused - one out of fuel, or being used by
            // somebody else - would repair nothing through vanilla and then everything through
            // here, which is the mod visibly breaking a rule the game just enforced.
            //
            // They also stand in for "did vanilla actually repair something". It cannot be read
            // directly: RepairOneItem returns void and leaves no flag behind. But it repairs the
            // first worn item CanRepair accepts and returns, so with these two guards passed, any
            // item this loop finds is proof that vanilla found one first.
            CraftingStation station = player.GetCurrentCraftingStation();
            if (station == null && !player.NoCostCheat()) return;
            if (station != null && !station.CheckUsable(player, false)) return;

            Worn.Clear();
            player.GetInventory().GetWornItems(Worn);

            int repaired = 0;

            for (int i = 0; i < Worn.Count && repaired < budget; i++)
            {
                ItemDrop.ItemData item = Worn[i];
                if (item == null) continue;

                // The item vanilla just fixed is not in this list - GetWornItems only returns
                // items below their maximum durability, and that one is at it.
                if (!_canRepair(gui, item)) continue;

                // Vanilla's two lines, in vanilla's order, with the same skill term. The order
                // matters: the skill granted is how worn the item was, so reading it after the
                // durability write would grant nothing at all.
                player.RaiseSkill(Skills.SkillType.Crafting, 1f - item.m_durability / item.GetMaxDurability());
                item.m_durability = item.GetMaxDurability();

                repaired++;
            }

            Worn.Clear();

            if (SkaftConfig.Verbose.Value)
            {
                SkaftPlugin.Log.LogInfo(
                    "Bench: crafting " + Sweep.Level(player) + ", allowance " + (budget + 1)
                    + " items, " + (repaired + 1) + " repaired.");
            }

            if (repaired <= 0) return;

            // No effect call here. Vanilla fired m_repairItemDoneEffects once for the item it
            // repaired, and one press should sound like one press - the sweep makes the same
            // choice about the build effect for the same reason.
            //
            // The message is vanilla's key with a different noun in it. $msg_repaired is
            // "Repaired {0}", and handing it a count where it expects an item name gives
            // "Repaired 9 items" in every language that translated the verb, with no new
            // localization key and no string of this mod's own. The obvious alternative was the
            // sweep's trick - re-send the same text with an amount, and let MessageHud coalesce
            // it into "Repaired Bronze Axe x9" - and it is wrong here: the sweep's pieces really
            // are nine of the same wall, where a bench press fixes nine different things and
            // naming one of them nine times is a lie about which.
            //
            // Centre, not corner, because that is where vanilla put the line this replaces.
            // Centre messages supersede rather than queue, so this lands on top of vanilla's
            // "Repaired Bronze Axe" in the same frame instead of following it a second later.
            player.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$msg_repaired", (repaired + 1) + " items"));
        }

        /// <summary>
        /// True when vanilla's own repair gate is reachable. A failed binding costs this feature
        /// and nothing else - the sweep has its own binding and is unaffected - and says so once.
        /// </summary>
        private static bool Bind()
        {
            if (_bound) return !_bindFailed;
            _bound = true;

            try
            {
                // An open instance delegate: the first argument is the InventoryGui the postfix
                // was handed, so nothing here has to find or cache the singleton.
                _canRepair = AccessTools.MethodDelegate<Func<InventoryGui, ItemDrop.ItemData, bool>>(
                    AccessTools.Method(typeof(InventoryGui), "CanRepair"));

                if (_canRepair == null) throw new MissingMemberException();
            }
            catch (Exception e)
            {
                _bindFailed = true;
                SkaftPlugin.Log.LogWarning(
                    "Could not reach InventoryGui.CanRepair - the bench repairs one item a press, "
                    + "as vanilla does. The hammer sweep is unaffected. " + e.Message);
            }

            return !_bindFailed;
        }
    }
}
