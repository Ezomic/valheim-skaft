using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Skaft
{
    /// <summary>
    /// The radius, and the sweep it drives.
    ///
    /// The whole mod is one idea: the Crafting skill buys reach, and never a discount. Every
    /// piece the sweep touches is charged exactly what vanilla charges to repair one piece by
    /// hand, so the radius says how far you may reach and the stamina bar says how many you may
    /// fix. That is what keeps a wide reach from being the same thing as free maintenance,
    /// which is what every other area-repair mod ends up being.
    ///
    /// Nothing here runs unless <see cref="SkaftPatches"/> has already established that vanilla
    /// itself just repaired the piece under the cursor. See the gate there - it is why this file
    /// carries no permission logic for the hovered piece and no build-mode checks.
    /// </summary>
    internal static class Sweep
    {
        /// <summary>
        /// Reused between swings. WearNTear.GetAllInstances() hands back the live static list,
        /// ZNetScene spawns and destroys pieces every 1/30s as the player moves, and both
        /// registries remove by swap-with-last - so iterating the original while repairing is an
        /// InvalidOperationException waiting for a busy frame, and no index into it survives.
        /// Copy first, always.
        /// </summary>
        private static readonly List<WearNTear> Candidates = new List<WearNTear>();

        /// <summary>
        /// Held in a field rather than captured in a lambda so the sort does not allocate a new
        /// closure and delegate on every swing.
        /// </summary>
        private static Vector3 _center;

        private static readonly Comparison<WearNTear> ByDistance = (a, b) =>
            (a.transform.position - _center).sqrMagnitude
                .CompareTo((b.transform.position - _center).sqrMagnitude);

        // Reflection, bound lazily and never in a static initializer. A FieldRef bound in a
        // static field throws at type-init when the name or owner is wrong, and from then on
        // every Harmony patch in the class throws TypeInitializationException - which presents
        // as unrelated vanilla features breaking rather than as this mod failing. Bound here, a
        // bad binding costs the sweep and nothing else.
        private static AccessTools.FieldRef<WearNTear, float> _lastRepair;
        private static Func<Player, float> _buildStamina;
        private static bool _bound;
        private static bool _bindFailed;

        /// <summary>
        /// Reach in metres for this player right now, measured from the piece under the cursor.
        ///
        /// GetSkillLevel applies the status-effect modifier to the raw level and only then
        /// floors it, so it is always an integer but is bounded at neither end - a skill-raising
        /// food pushes it above 100 and a debuff below 0. GetSkillFactor is that same number
        /// over 100, Clamp01'd, so multiplying it back up is the integral 0..100 this wants and
        /// the reason a food cannot push the radius past MaxRadius.
        ///
        /// Read per swing, never cached. Dying costs a quarter of the level and wipes the
        /// accumulator, so this number moves down mid-session as well as up.
        /// </summary>
        internal static float Radius(Player player)
        {
            float level = player.GetSkillFactor(Skills.SkillType.Crafting) * 100f;

            float full = Mathf.Max(1f, SkaftConfig.FullLevel.Value);
            float t = Mathf.Pow(Mathf.Clamp01(level / full), Mathf.Max(0.01f, SkaftConfig.Curve.Value));

            return Mathf.Lerp(SkaftConfig.MinRadius.Value, SkaftConfig.MaxRadius.Value, t);
        }

        /// <summary>Crafting level as the build menu shows it - the same number the radius used.</summary>
        internal static int Level(Player player)
        {
            return Mathf.RoundToInt(player.GetSkillFactor(Skills.SkillType.Crafting) * 100f);
        }

        /// <summary>
        /// Repair everything within reach of <paramref name="hovered"/>, charging the vanilla
        /// per-piece cost for each one, and tell the player how many that was.
        /// </summary>
        internal static void Run(Player player, ItemDrop.ItemData tool, Piece hovered, WearNTear hoveredWear)
        {
            float radius = Radius(player);
            if (radius <= 0f) return;

            if (!Bind()) return;

            _center = hovered.transform.position;
            float radiusSqr = radius * radius;

            Candidates.Clear();
            List<WearNTear> all = WearNTear.GetAllInstances();
            for (int i = 0; i < all.Count; i++)
            {
                WearNTear wear = all[i];
                if (wear == null || wear == hoveredWear) continue;
                if ((wear.transform.position - _center).sqrMagnitude > radiusSqr) continue;

                // Cheapest and most selective test there is, so it belongs here rather than
                // after the sort and four component lookups. On a base that is standing, nearly
                // everything in radius is intact, and this keeps the sort and every per-piece
                // scan down to the pieces a swing could actually do something to. It is a cache
                // rather than the authority, so the real check stays in the repair loop as well.
                if (wear.GetHealthPercentage() >= 1f) continue;

                Candidates.Add(wear);
            }

            // Nearest first. The wallet truncates the sweep, so the order decides which pieces
            // get fixed, and "the wall in front of me" is the only defensible answer to that.
            Candidates.Sort(ByDistance);

            float cost = Mathf.Max(0f, SkaftConfig.CostMultiplier.Value);
            float stamina = _buildStamina(player) * cost;
            float eitr = tool.m_shared.m_attack.m_attackEitr * cost;
            float drain = tool.m_shared.m_useDurabilityDrain * cost;

            int repaired = 0;
            string stopped = "radius";

            for (int i = 0; i < Candidates.Count; i++)
            {
                if (repaired >= SkaftConfig.MaxPieces.Value) { stopped = "MaxPieces"; break; }

                // HaveStamina compares against the raw number; UseStamina multiplies by
                // Game.m_staminaRate before subtracting. Without the same multiplier here the
                // gate and the charge disagree the moment a server sets a StaminaRate global
                // key, and the sweep either stops early or runs on an empty bar.
                if (!player.HaveStamina(stamina * Game.m_staminaRate)) { stopped = "stamina"; break; }

                // Repair subtracts durability with no clamp and no zero check, so a wide sweep
                // can spend a whole hammer inside one frame. The break itself is not silent -
                // Humanoid.DrainEquipedItemDurability messages "$msg_broke" with the item icon
                // on the next FixedUpdate, because UpdateEquipment calls it for any right-hand
                // item with m_useDurability whatever its passive drain is - but it also unequips,
                // and being dropped out of build mode by one press is a different event from
                // wearing a tool down over the swings that did it. The sweep never breaks a
                // tool; ordinary swinging still breaks it when it should.
                if (tool.m_shared.m_useDurability
                    && tool.m_durability - drain <= SkaftConfig.DurabilityFloor.Value)
                {
                    stopped = "durability";
                    break;
                }

                WearNTear wear = Candidates[i];
                if (wear == null) continue;

                // s_allInstances carries WearNTear objects that are not build pieces at all -
                // Ashlands altars, dvergr shelves. Vanilla's hammer can never target one:
                // UpdateWearNTearHover resolves GetComponentInParent<Piece>(), so an object with
                // no Piece never becomes m_hoveringPiece. Without this the mod silently heals
                // dvergr ruins.
                if (!wear.TryGetComponent(out Piece piece)) continue;

                ZNetView nview = wear.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) continue;

                if (SkaftConfig.OwnBuildingsOnly.Value
                    && nview.GetZDO().GetLong(ZDOVars.s_creator, 0L) != player.GetPlayerID())
                {
                    continue;
                }

                // CheckCanRemovePiece's condition, inlined without its Message call. That method
                // is private, and it writes "$msg_missingstation" centre-screen with no rate
                // limit, so calling it per piece turns one missing workbench into a wall of
                // messages. Vanilla already ran it once, for the hovered piece.
                //
                // NoCostCheat() first, because that is the term vanilla short-circuits on and
                // dropping it makes the sweep stricter than the swing that started it: under
                // nocost the hovered wall repairs and every station-gated wall beside it would
                // silently refuse, which reads as the mod being broken by a debug command.
                if (!player.NoCostCheat()
                    && piece.m_craftingStation != null
                    && CraftingStation.HaveBuildStationInRange(
                           piece.m_craftingStation.m_name, player.transform.position) == null
                    && !(ZoneSystem.instance != null
                         && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)))
                {
                    continue;
                }

                // Per piece, not once for the centre: an 8m sphere routinely crosses a ward
                // boundary. flash:false is not optional - the default strobes every denying ward
                // for every player nearby, once per piece. Never wardCheck:true; that inverts the
                // access test into an AND and exactly one vanilla caller passes it, placing a
                // ward. Refusals are silent, which is also what vanilla does on a warded repair.
                if (!PrivateArea.CheckAccess(piece.transform.position, 0f, false)) continue;

                // A hint, not a gate. m_healthPercentage is a cache fed by RPC_HealthChanged;
                // Repair() re-reads the ZDO itself and is the authority.
                if (wear.GetHealthPercentage() >= 1f) continue;

                // InvokeRPC(string) targets m_zdo.GetOwner(), which is 0 when nobody owns the
                // piece - and 0 means broadcast. Every recipient then fails RPC_Repair's own
                // IsOwner() test while Repair() has already returned true, so without this claim
                // the mod charges stamina and durability for repairs that never happened.
                if (!nview.HasOwner()) nview.ClaimOwnership();

                // False covers three cases with no reason code: invalid view, already at full
                // health, and inside the piece's own one-second repair cooldown. All three are
                // "nothing to do" - no charge, no count.
                if (!wear.Repair()) continue;

                player.UseStamina(stamina);
                player.UseEitr(eitr);
                if (tool.m_shared.m_useDurability) tool.m_durability -= drain;

                repaired++;
            }

            if (SkaftConfig.Verbose.Value)
            {
                SkaftPlugin.Log.LogInfo(
                    "Sweep: crafting " + Level(player) + ", radius " + radius.ToString("0.0")
                    + "m, " + Candidates.Count + " damaged in range, " + repaired
                    + " repaired, stopped on " + stopped + ".");
            }

            Candidates.Clear();

            if (repaired <= 0) return;

            // Vanilla queued "$msg_repaired <piece name>" for the hovered piece with amount 0.
            // MessageHud coalesces a queued TopLeft message into the current one when the text
            // and icon match and the current is under four seconds old, and it sums their
            // amounts, rendering " xN" for anything above 1. So re-sending the same text with a
            // count turns the pair into one line reading "Repaired Wood wall x13" - already
            // translated, no new localization key, and no per-piece message spam. The count is
            // the radius readout, in the only unit that matters.
            player.Message(MessageHud.MessageType.TopLeft,
                Localization.instance.Localize("$msg_repaired", hovered.m_name),
                repaired + 1);
        }

        /// <summary>
        /// Whether vanilla's own repair just succeeded on this piece, this frame.
        ///
        /// Player.Repair's success flag is a stack local with no field, out-param or event
        /// behind it, so a postfix cannot read it. WearNTear stamps m_lastRepair only on
        /// Repair()'s success path, which is the same fact - owner-independent, unlike a ZDO
        /// health read, which is stale on a piece someone else owns, and live, unlike
        /// GetHealthPercentage, which is a cache fed by an RPC that has not arrived yet.
        /// </summary>
        internal static bool JustRepaired(WearNTear wear)
        {
            if (!Bind()) return false;

            try { return _lastRepair(wear) == Time.time; }
            catch { return false; }
        }

        /// <summary>
        /// True when the private members the sweep needs are reachable. A failed binding costs
        /// the feature and nothing else, and says so once.
        /// </summary>
        private static bool Bind()
        {
            if (_bound) return !_bindFailed;
            _bound = true;

            try
            {
                _lastRepair = AccessTools.FieldRefAccess<WearNTear, float>("m_lastRepair");

                _buildStamina = AccessTools.MethodDelegate<Func<Player, float>>(
                    AccessTools.Method(typeof(Player), "GetBuildStamina"));

                if (_lastRepair == null || _buildStamina == null) throw new MissingMemberException();
            }
            catch (Exception e)
            {
                _bindFailed = true;
                SkaftPlugin.Log.LogWarning(
                    "Could not reach Player.GetBuildStamina or WearNTear.m_lastRepair - the "
                    + "sweep is off and repair stays vanilla. " + e.Message);
            }

            return !_bindFailed;
        }
    }
}
