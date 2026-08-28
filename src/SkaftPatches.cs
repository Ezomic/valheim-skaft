using HarmonyLib;
using UnityEngine;

namespace Skaft
{
    /// <summary>
    /// Two postfixes. No prefix, no transpiler, no second entry point.
    ///
    /// The design argument for both of them is the same one: ride vanilla rather than
    /// re-deriving it. Player.Repair already checks build mode, resolves the hovered piece,
    /// runs CheckCanRemovePiece and PrivateArea.CheckAccess on it, refuses a piece at full
    /// health and honours WearNTear's one-second cooldown. A prefix that replaced the method
    /// would own copies of all six, and would own them again after every game update. A
    /// postfix that only asks "did that actually repair something" inherits the lot, including
    /// whatever guard a future update adds.
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

        /// <summary>How long between reach-line refreshes, in seconds.</summary>
        private const float ReachInterval = 1f;

        private static float _nextReach;
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
            if (!SkaftConfig.IsReachEntry(Utils.GetPrefabName(selected.gameObject.name))) return;

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
