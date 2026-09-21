using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Skaft
{
    /// <summary>
    /// Four postfixes. No prefix, no transpiler, no second entry point.
    ///
    /// The design argument for all of them is the same one: ride vanilla rather than
    /// re-deriving it. Player.Repair already checks build mode, resolves the hovered piece,
    /// runs CheckCanRemovePiece and PrivateArea.CheckAccess on it, refuses a piece at full
    /// health and honours WearNTear's one-second cooldown. A prefix that replaced the method
    /// would own copies of all six, and would own them again after every game update. A
    /// postfix that only asks "did that actually repair something" inherits the lot, including
    /// whatever guard a future update adds.
    ///
    /// InventoryGui.RepairOneItem is the same arrangement one verb along: it has already found
    /// a usable station and let CanRepair pick the item, so a postfix that fixes the rest of
    /// the press's allowance inherits the recipe lookup, the station match, the station level
    /// and the world level without holding a copy of any of them.
    /// </summary>
    internal static class SkaftPatches
    {
        /// <summary>
        /// The mod, in one method: after vanilla repairs the piece under your cursor, repair
        /// everything else within reach of it and charge for each one.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Repair", new[] { typeof(ItemDrop.ItemData), typeof(Piece) })]
        private static void Repair(Player __instance, ItemDrop.ItemData toolItem)
        {
            if (!SkaftConfig.Enabled.Value) return;

            // Every player object in the scene runs the patched method, not only yours.
            if (__instance == null || __instance != Player.m_localPlayer) return;
            if (toolItem == null) return;

            // The argument is not the target. Player.Repair ignores its repairPiece parameter
            // entirely - that is the build menu's repair entry, not the thing being hit - and
            // reads GetHoveringPiece() itself. Passing the argument through here would sweep
            // around a UI element at the origin.
            Piece hovered = __instance.GetHoveringPiece();
            if (hovered == null) return;

            if (!hovered.TryGetComponent(out WearNTear hoveredWear)) return;

            // The gate, and the whole reason this is a postfix. m_lastRepair is stamped only on
            // WearNTear.Repair()'s success path, so this is true exactly when vanilla just did
            // the work - which means every guard vanilla ran has already passed.
            //
            // The consequence is worth saying out loud, because it will be reported as a bug:
            // the sweep runs only when the piece under the cursor was itself damaged and off
            // its own one-second cooldown. Hovering an intact wall beside a broken one does
            // nothing, and a second click inside one second does nothing. Point at something
            // broken. That is the trigger rule, and it buys the correctness above.
            //
            // It also makes this mod inert wherever another patch skips the original - Vaettir's
            // Transplant prefixes this same method and returns false for the cultivator's own
            // tool piece. Postfixes still run after a skipping prefix, but m_lastRepair never
            // moved, so nothing sweeps. That is why there is no config listing which tools may
            // sweep: the gate answers it for free.
            if (!Sweep.JustRepaired(hoveredWear)) return;

            Sweep.Run(__instance, toolItem, hovered, hoveredWear);
        }

        /// <summary>
        /// The bench half: after vanilla repairs the first worn item, repair the rest of what
        /// this player's Crafting allows in the same press.
        ///
        /// Patched on RepairOneItem rather than on OnRepairPressed, which is the method the
        /// button actually calls. OnRepairPressed also runs UpdateRepair and UpdateCraftingPanel
        /// afterwards, and both of those re-read whether anything repairable is left - so sitting
        /// inside the pair means the button's glow and the panel are refreshed against the state
        /// this left behind, for free. A postfix on the outer method would repair after they had
        /// already drawn, and the button would keep glowing for one more press.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "RepairOneItem")]
        private static void RepairOneItem(InventoryGui __instance)
        {
            if (!SkaftConfig.Enabled.Value || !SkaftConfig.RepairItems.Value) return;

            Bench.Run(__instance);
        }

        /// <summary>How long between damaged-count refreshes while the cursor holds still.</summary>
        private const float CountInterval = 0.25f;

        private static float _nextCount;
        private static WearNTear _countedFor;
        private static string _countLine;

        /// <summary>
        /// Puts the number of damaged pieces in reach under the crosshair, while a repair entry
        /// is selected and you are pointing at a piece.
        ///
        /// This answers the mod's most-asked question and its first troubleshooting entry at the
        /// same time, and the second one is why it exists. A piece above 75% health looks
        /// perfect: WearNTear.UpdateVisual only swaps in the worn model below 0.75 and the broken
        /// one below 0.25, so the top quarter of every health bar is invisible damage. A player
        /// hunting for what to hit therefore cannot see it, and a swing at an intact wall beside
        /// a damaged one does nothing at all, because the sweep only follows a repair vanilla
        /// itself just made. Both of those read as the mod being broken. One line says otherwise.
        ///
        /// Written into m_hoverName rather than anywhere of ours. Vanilla rewrites that field
        /// every frame from the hovered object's own hover text - and leaves it empty for a plain
        /// wall, which has no Hoverable - so appending costs no state, needs no undo, and cannot
        /// survive the mod being switched off mid-session the way the build menu line can.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
        private static void UpdateCrosshair(Hud __instance, Player player)
        {
            if (!SkaftConfig.Enabled.Value || !SkaftConfig.ShowDamagedInReach.Value) return;
            if (__instance == null || player == null || player != Player.m_localPlayer) return;

            var label = __instance.m_hoverName;
            if (label == null) return;

            // Same gate as the build menu line, and for the same reason: m_repairPiece means
            // "this entry clicks on the world" rather than "this is the hammer's Repair", and
            // the sweep provably cannot run on another mod's tool. A count written there would
            // describe something that is never going to happen.
            Piece selected = player.GetSelectedPiece();
            if (selected == null || !selected.m_repairPiece
                || !SkaftConfig.IsReachEntry(Utils.GetPrefabName(selected.gameObject.name)))
            {
                _countLine = null;
                _countedFor = null;
                return;
            }

            Piece hovered = player.GetHoveringPiece();
            if (hovered == null || !hovered.TryGetComponent(out WearNTear hoveredWear))
            {
                _countLine = null;
                _countedFor = null;
                return;
            }

            float radius = Sweep.Radius(player);
            if (radius <= 0f)
            {
                // Below the curve there is no sweep, so there is nothing to report. Saying
                // "0 in reach" would describe a radius rather than a base, and a new character
                // should not be told about a feature they do not have yet.
                _countLine = null;
                _countedFor = null;
                return;
            }

            // Keyed on the piece as well as the clock. A throttle alone would leave the line
            // describing whatever was under the cursor a quarter of a second ago, which is a
            // different wall every time the mouse moves - so a new piece recounts at once and
            // only a cursor holding still falls back to the interval.
            if (hoveredWear != _countedFor || Time.time >= _nextCount)
            {
                _countedFor = hoveredWear;
                _nextCount = Time.time + CountInterval;

                int around = Sweep.CountDamaged(hovered.transform.position, radius, hoveredWear);

                // The piece under the cursor is vanilla's to repair and the sweep's trigger, so
                // it is counted when it is damaged and named when it is not. Intact, the swing
                // does nothing whatever is standing broken around it, and that is the one rule
                // of this mod a player has to be told rather than shown.
                bool intact = hoveredWear.GetHealthPercentage() >= 1f;
                int total = around + (intact ? 0 : 1);

                _countLine = total <= 0
                    ? "Damaged in reach: none"
                    : "Damaged in reach: " + total + (intact ? " (aim at a damaged piece)" : "");
            }

            if (string.IsNullOrEmpty(_countLine)) return;

            label.text = string.IsNullOrEmpty(label.text) ? _countLine : label.text + "\n" + _countLine;
        }

        /// <summary>How long between reach-line refreshes, in seconds.</summary>
        private const float ReachInterval = 1f;

        private static float _nextReach;

        /// <summary>Repair entries already named in the log, so each is reported once.</summary>
        private static readonly HashSet<string> _unknownEntries = new HashSet<string>();

        private static Piece _described;
        private static string _originalDescription;
        private static string _writtenDescription;

        /// <summary>
        /// Puts the current reach on the Repair entry in the build menu.
        ///
        /// No keybind, no window, no ring. The build menu is where a player already looks to
        /// find out what a build-menu entry does, Hud.SetupPieceInfo re-reads m_description and
        /// re-localizes it every time the panel updates, and the line therefore corrects itself
        /// across a world reload without any state of ours surviving.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdatePlacement", new[] { typeof(bool), typeof(float) })]
        private static void UpdatePlacement(Player __instance)
        {
            if (__instance == null || __instance != Player.m_localPlayer) return;

            // The selection read and the undo sit above every other guard on purpose. Restore()
            // is the only thing that takes our line back off the prefab, so anything returning
            // ahead of it freezes a stale reach on an entry nothing is updating any more. That
            // is not hypothetical: Core writes a host's Enabled straight into the entry while
            // the game is running, and the config manager does the same, so "the flag cannot
            // change mid-session" is exactly the assumption that leaves the line stuck.
            //
            // Reading the selection every frame rather than once a second is two list lookups.
            // GetSelectedPiece is null-safe on the same field InPlaceMode() tests - it IS
            // m_buildPieces != null - so putting the hammer away arrives here as a null
            // selection and restores, instead of returning early and leaving the write behind.
            Piece selected = __instance.GetSelectedPiece();
            if (selected != _described) Restore();

            if (!SkaftConfig.Enabled.Value || !SkaftConfig.ShowReachInBuildMenu.Value)
            {
                Restore();
                return;
            }

            if (selected == null || !selected.m_repairPiece) return;

            // m_repairPiece does not mean "the hammer's Repair button". It means "this entry
            // clicks on the world instead of placing into it", and other mods hang their own
            // tools off it - Vaettir's Transplant entry on the cultivator wears it. The sweep
            // provably cannot run there: Transplant's prefix skips vanilla's Repair, so
            // m_lastRepair never moves and the gate above is never satisfied. Writing a reach on
            // it would advertise a number that describes nothing.
            string entry = Utils.GetPrefabName(selected.gameObject.name);
            if (!SkaftConfig.IsReachEntry(entry))
            {
                // Names it once, because ReachEntries defaults to a name that cannot be checked
                // any other way: piece_repair is not registered in ZNetScene or ObjectDB, so a
                // Devkit rip cannot resolve it and nothing in the decompiled assembly carries it
                // either. If the default is wrong the reach line silently never appears, which is
                // indistinguishable from the feature being off - so the log says what was
                // actually selected and the fix is one config edit.
                if (SkaftConfig.Verbose.Value && _unknownEntries.Add(entry))
                {
                    SkaftPlugin.Log.LogInfo(
                        "Repair entry '" + entry + "' is not in ReachEntries, so no reach line is "
                        + "written on it. That is correct for another mod's tool; add the name if "
                        + "it is a repair entry the sweep can actually serve.");
                }

                return;
            }

            if (Time.time < _nextReach) return;
            _nextReach = Time.time + ReachInterval;

            if (_described == null)
            {
                _described = selected;
                _originalDescription = selected.m_description;
            }

            // Somebody else - another mod, or a language change - has written to the field since
            // we last did. Treat what is there now as the original rather than stacking on it.
            if (selected.m_description != _writtenDescription && selected.m_description != null
                && selected.m_description != _originalDescription)
            {
                _originalDescription = selected.m_description;
            }

            string line = "Reach: " + Sweep.Radius(__instance).ToString("0.0") + "m (Crafting "
                          + Sweep.Level(__instance) + ")";

            // Set, never append. This runs once a second for as long as the entry is selected,
            // and appending would grow the description until it filled the panel.
            _writtenDescription = string.IsNullOrEmpty(_originalDescription)
                ? line
                : _originalDescription + "\n" + line;

            selected.m_description = _writtenDescription;
        }

        /// <summary>
        /// Hand the repair entry its own description back.
        ///
        /// m_description is a field on the shared prefab, so this is the client's in-memory copy
        /// for the session - no ZDO, nothing another player sees. It still has to be undone,
        /// because leaving a stale reach on an entry nobody is looking at is a small lie, and
        /// because the mod being disabled at runtime should look like the mod being absent.
        /// </summary>
        internal static void Restore()
        {
            if (_described != null && _described.m_description == _writtenDescription)
            {
                _described.m_description = _originalDescription;
            }

            _described = null;
            _originalDescription = null;
            _writtenDescription = null;
        }
    }
}
