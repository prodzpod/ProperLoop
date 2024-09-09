/*
using EntityStates.ScavBackpack;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using WellRoundedBalance.Interactables;
using WellRoundedBalance.Mechanics.Director;

namespace ProperLoop
{
    public class WRB
    {
        [HarmonyPatch(typeof(ScavengerBag), nameof(ScavengerBag.Opening_OnEnter))]
        public class PatchWRBSBag
        {
            public static bool Prefix(On.EntityStates.ScavBackpack.Opening.orig_OnEnter orig, Opening self)
            {
                if (Main.ScavItemCountScale.Value)
                {
                    Opening.maxItemDropCount = Main.loops * RoR2.Run.stagesPerLoop + Main.stage + 1;
                    orig(self);
                    return false;
                } // bad wrb
                else return true;
            }
        }

        [HarmonyPatch(typeof(SceneDirector), nameof(SceneDirector.SceneDirector_onPrePopulateMonstersSceneServer))]
        internal class PatchWRBDirector
        {
            public static void ILManipulator(ILContext il)
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchStloc(out _));
                c.Emit(OpCodes.Pop);
                c.EmitDelegate(() => Main.stage);
            }
        }
    }
}
*/