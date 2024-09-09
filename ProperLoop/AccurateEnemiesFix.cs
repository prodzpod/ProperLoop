/*
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Reflection;
using AccurateEnemies.Hooks;
using System;

namespace ProperLoop
{
    public class AccurateEnemiesFix
    {
        public static void Init()
        {
            Main.Harmony.PatchAll(typeof(BeetleGuardFix));
            Main.Harmony.PatchAll(typeof(BronzongFix));
            Main.Harmony.PatchAll(typeof(ClayBossFix));
            Main.Harmony.PatchAll(typeof(ClayGrenadierFix));
            Main.Harmony.PatchAll(typeof(FlyingVerminFix));
            Main.Harmony.PatchAll(typeof(GreaterWispFix));
            Main.Harmony.PatchAll(typeof(GrovetenderFix));
            Main.Harmony.PatchAll(typeof(LemurianFix));
            Main.Harmony.PatchAll(typeof(LemurianBruiserFix));
            Main.Harmony.PatchAll(typeof(LunarExploderFix));
            Main.Harmony.PatchAll(typeof(MinorConstructFix));
            Main.Harmony.PatchAll(typeof(RoboBallBossFix));
            Main.Harmony.PatchAll(typeof(ScavengerFix));
            Main.Harmony.PatchAll(typeof(VagrantFix));
            Main.Harmony.PatchAll(typeof(VoidJailerFix));
            Main.Harmony.PatchAll(typeof(VultureFix));
        }
        public static void Patch(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<Run>(nameof(Run.stageClearCount))))
            {
                c.Emit(OpCodes.Pop);
                c.EmitDelegate(() => Main.loops > 0 ? 5 : 0);
            }
        }
        public static MethodBase GetMethod(Type t) => AccessTools.DeclaredMethod(t.GetNestedType("<>c", AccessTools.all), "<Init>b__3_1") ?? AccessTools.DeclaredMethod(t.GetNestedType("<>c", AccessTools.all), "<Init>b__3_0");

        [HarmonyPatch] public class BeetleGuardFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(BeetleGuard)); }
        [HarmonyPatch] public class BronzongFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(Bronzong)); }
        [HarmonyPatch] public class ClayBossFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(ClayBoss)); }
        [HarmonyPatch] public class ClayGrenadierFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(ClayGrenadier)); }
        [HarmonyPatch] public class FlyingVerminFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(FlyingVermin)); }
        [HarmonyPatch] public class GreaterWispFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(GreaterWisp)); }
        [HarmonyPatch] public class GrovetenderFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(Grovetender)); }
        [HarmonyPatch] public class LemurianFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(Lemurian)); }
        [HarmonyPatch] public class LemurianBruiserFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(LemurianBruiser)); }
        [HarmonyPatch] public class LunarExploderFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(LunarExploder)); }
        [HarmonyPatch] public class MinorConstructFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(MinorConstruct)); }
        [HarmonyPatch] public class RoboBallBossFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(RoboBallBoss)); }
        [HarmonyPatch] public class ScavengerFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(Scavenger)); }
        [HarmonyPatch] public class VagrantFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(Vagrant)); }
        [HarmonyPatch] public class VoidJailerFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(VoidJailer)); }
        [HarmonyPatch] public class VultureFix { public static void ILManipulator(ILContext il) => Patch(il); public static MethodBase TargetMethod() => GetMethod(typeof(Vulture)); }
    }
}
*/