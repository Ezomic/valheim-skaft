using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Ezomic.Core;
using HarmonyLib;

namespace Skaft
{
    /// <summary>
    /// Skaft. Repairing with the hammer also repairs the pieces around what you hit, and how far
    /// that reaches is your Crafting skill.
    ///
    /// Area repair already exists and it is popular, and the reason is that maintaining a base
    /// one wall at a time after a troll walks through it is tedious rather than interesting. The
    /// problem with the versions on offer is not the radius, it is that the radius is a config
    /// entry: you install the mod and maintenance is over. This one hands out the same
    /// convenience but makes it something the character earns, and never makes it free. Reach
    /// grows with Crafting, from nothing at all at level 0 to the length of a longhouse around
    /// level 60; every piece the sweep touches still costs the stamina and the hammer wear that
    /// repairing it by hand would have cost. So the skill decides how far you can reach and your
    /// stamina bar decides how much you can afford, which is a different mod from "buildings no
    /// longer need repairing".
    ///
    /// The obvious alternative was to scale the *cost* down with skill instead, or to key the
    /// radius on a config number and be done. Both were rejected for the same reason: they end
    /// at maintenance being free, one immediately and one eventually.
    ///
    /// Client-side, in the honest sense: every decision is made by the player swinging the
    /// hammer, off state that client already has, and the only thing that leaves the machine is
    /// the RPC vanilla would have sent anyway. A player without this mod sees an identical
    /// world - hence Requirement.HostOnly, so a guest without it can still join.
    ///
    /// It could not be enforced server-side even if that were wanted. WearNTear.RPC_Repair
    /// ignores its sender argument and has no permission check of any kind, and ZRoutedRpc on
    /// the server forwards without inspecting method, ZDO or sender - so any client can already
    /// restore any loaded piece. Enforcing a radius would mean a custom RPC, a validator and a
    /// skill claim the server has to trust or re-derive: a second code path and a class of
    /// desync bugs, spent constraining something that was never constrained.
    ///
    /// There is deliberately no BepInProcess attribute. A dedicated server runs
    /// valheim_server.exe, and Core's gate only refuses on the server side of RPC_PeerInfo - so
    /// a mod that must be enforced has to be allowed to load there.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Soft, not hard. A hard dependency that is absent does not degrade - the plugin never
    // loads at all - and every mod here has to be installable on its own, because a stranger
    // should not need two installs to get one mod. Soft still buys the load-order guarantee
    // when Core is present, which is all that registering with the gate needs.
    [BepInDependency(CoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class SkaftPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.skaft";
        public const string PluginName = "Skaft";
        public const string PluginVersion = "0.1.0";
        public const string PluginAuthor = "Robbin Thijssen";

        /// <summary>Core's plugin GUID. Optional - see TryRegisterWithCore.</summary>
        private const string CoreGuid = "ezomic.valheim.core";

        internal static ManualLogSource Log;

        /// <summary>
        /// Whether Core answered at load. Worth keeping even when nothing reads it yet: the
        /// difference between gated and ungated is invisible to a player otherwise, and this is
        /// what a warning on spawn would be driven by.
        /// </summary>
        internal static bool CorePresent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config first. Registering absorbs every entry the mod has bound, so anything bound
            // after this line is carried only because Core re-absorbs at manifest time - and
            // depending on the order of two lines in an Awake is not a thing worth relying on.
            SkaftConfig.Bind(Config);

            TryRegisterWithCore();

            // PatchAll over a named type, never the whole assembly. A bare PatchAll() walks every
            // type in the DLL, so a half-written patch class in another file goes live the moment
            // it compiles.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SkaftPatches));

            // The startup line every mod in the suite writes. It is how a log answers "which
            // build of what is actually loaded" without anyone guessing.
            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        /// <summary>
        /// Joins Core's version gate when Core is installed, and does nothing when it is not.
        ///
        /// Standing alone costs the curve, not the mod. Without Core nothing swaps the host's
        /// radius settings into a joining client, so on a shared server the reach becomes an
        /// agreement between players rather than a property of the world - anyone can set
        /// MaxRadius to 100 and CostMultiplier to 0 in their own file and have exactly the mod
        /// this one was written not to be. That is a real loss and it is the server owner's
        /// choice to accept, which is why this logs rather than refusing to run.
        /// </summary>
        private void TryRegisterWithCore()
        {
            CorePresent = Chainloader.PluginInfos.ContainsKey(CoreGuid);

            if (!CorePresent)
            {
                Log.LogInfo("Core not installed - running standalone, without the version gate.");
                return;
            }

            RegisterWithCore();
        }

        /// <summary>
        /// Kept separate and never inlined on purpose. The JIT resolves the assemblies a method
        /// needs when it first compiles that method, so a Suite call sitting directly in Awake
        /// would drag Ezomic.Core in before the check above could prevent it - and the
        /// missing-assembly exception would land during plugin load, which is the exact failure
        /// this arrangement exists to avoid.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterWithCore()
        {
            // HostOnly, and it is not a preference. This mod registers no prefab, writes no ZDO
            // key it invented, changes no item data and sends no RPC vanilla does not already
            // send, so a client without it is genuinely unaffected - it just repairs one piece
            // at a swing. Everyone would refuse those clients for nothing.
            //
            // HostOnly still checks clients that DO have it against the host, which is the half
            // that matters here: it is what makes the curve the host's to set.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config, Requirement.HostOnly);

            // Registering already absorbs the whole config file, so naming these is a formality.
            // It is worth writing anyway: these five are the mod's balance, and saying out loud
            // that the host owns them is the point of putting Skaft on a server at all.
            Suite.Sync(SkaftConfig.Enabled, SkaftConfig.MinRadius, SkaftConfig.MaxRadius,
                       SkaftConfig.FullLevel, SkaftConfig.Curve, SkaftConfig.CostMultiplier);

            // Opting two back out. Both are display and diagnostics rather than balance, and a
            // host reaching across to turn off someone's build-menu line, or to switch on their
            // logging for the evening, is not a thing anybody asked for.
            Suite.Local(SkaftConfig.ShowReachInBuildMenu, SkaftConfig.Verbose);
        }

        private void OnDestroy()
        {
            // The build menu's repair entry carries a line we wrote into the shared prefab. Put
            // it back before the patches go, or a reload leaves a stale reach on an entry that
            // nothing is updating any more.
            SkaftPatches.Restore();

            // UnpatchSelf, never UnpatchAll(). The argumentless one unpatches every mod in the
            // process, not just this one.
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
