using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Reflection;
using UnityEngine;
using System.Linq;
using R2API;
using static RoR2.CombatDirector;
using BepInEx.Bootstrap;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System;
using EntityStates.ScavBackpack;
using System.IO;
// using static ProperLoop.WRB;

namespace ProperLoop
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(EliteAPI.PluginGUID)]
    [BepInDependency("com.Moffein.AccurateEnemies", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TPDespair.ZetArtifacts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("BALLS.WellRoundedBalance", BepInDependency.DependencyFlags.SoftDependency)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "ProperLoop";
        public const string PluginVersion = "1.0.13";
        public static ManualLogSource Log;
        public static PluginInfo pluginInfo;
        public static Harmony Harmony;
        public static ConfigFile Config;
        public static ConfigEntry<int> ScavCost;
        public static ConfigEntry<int> ScavOnLevel;
        public static ConfigEntry<bool> ScavItemCountScale;
        public static ConfigEntry<string> EliteDisables;
        public static ConfigEntry<float> T1Cost;
        public static ConfigEntry<int> T1OnLevel;
        public static ConfigEntry<float> T2Cost;
        public static ConfigEntry<int> T2OnLevel;
        public static ConfigEntry<bool> T2OnHonor;
        public static ConfigEntry<int> LoopBossesOnLevel;
        public static ConfigEntry<int> LoopEnemiesOnLevel;
        public static ConfigEntry<int> PerfectedOnLevel;
        public static ConfigEntry<bool> PerfectedOnHonor;
        public static ConfigEntry<float> PerfectedCost;
        public static ConfigEntry<float> HonorMultiplier;
        public static ConfigEntry<float> SanctionMultiplier;
        public static InteractableSpawnCard TP;
        public static InteractableSpawnCard lunarTP;
        private static bool _thisInitialized = false;
        private static WeightedSelection<EliteTierDef> EliteSelection = new();

        public static int loops = 0;
        public static int stage = 0;

        public void Awake()
        {
            pluginInfo = Info;
            Log = Logger;
            Harmony = new Harmony(PluginGUID);
            Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);
            ScavCost = Config.Bind("General", "Scavenger Cost", 2000, "Lower if you want scavs to spawn early. Vanilla by default.");
            ScavOnLevel = Config.Bind("General", "Scavenger Stage", 6, "5 per Proper Loop. 0 to disable.");
            ScavItemCountScale = Config.Bind("General", "Scavenger Item Count Scale", true, "Make scavenger sacks drop items proportional to current stage number.");
            T1Cost = Config.Bind("General", "Tier 1 Director Cost Multiplier", 1f, "Perfected on Moon also uses this.");
            T1OnLevel = Config.Bind("General", "Tier 1 Elites Stage", 1, "5 per Proper Loop. 0 to disable. May break modded artifacts if changed.");
            T2Cost = Config.Bind("General", "Tier 2 Director Cost Multiplier", 6f, "Vanilla value: 6.");
            T2OnLevel = Config.Bind("General", "Tier 2 Elites Stage", 6, "5 per Proper Loop. 0 to disable. May break modded artifacts if changed.");
            T2OnHonor = Config.Bind("General", "Tier 2 on Honor", true, "add T2 Elites to Honor. extra chaos!");
            LoopBossesOnLevel = Config.Bind("General", "Loop Bosses Stage", 6, "5 per Proper Loop. 0 to disable.");
            LoopEnemiesOnLevel = Config.Bind("General", "Loop Enemies Stage", 6, "5 per Proper Loop. 0 to disable.");
            PerfectedOnLevel = Config.Bind("General", "Perfected Elite Stage", 6, "5 per Proper Loop. 0 to disable. Enables perfected loop by default.");
            PerfectedOnHonor = Config.Bind("General", "Perfected on Honor", true, "add Perfected Elites to Honor. extra chaos!");
            PerfectedCost = Config.Bind("General", "Perfected Director Cost Multiplier", 6f, "By default, equal to T2.");
            HonorMultiplier = Config.Bind("General", "Artifact of Honor Stage Multiplier", 1f, "Rounded up.");
            SanctionMultiplier = Config.Bind("General", "Artifact of Sanction Stage Multiplier", 0.5f, "Epic zetartifacts compat");
            EliteDisables = Config.Bind("General", "Elite Disables", "", "List of EliteDef names to blacklist, separated by comma. Check log for names.");
            /*
            if (Chainloader.PluginInfos.ContainsKey("BALLS.WellRoundedBalance"))
            {
                Harmony.PatchAll(typeof(PatchWRBDirector));
                Harmony.PatchAll(typeof(PatchWRBSBag));
            }
            */
            // if (Chainloader.PluginInfos.ContainsKey("com.Moffein.AccurateEnemies")) AccurateEnemiesFix.Init();  
            On.RoR2.Run.Start += (orig, self) =>
            {
                loops = 0; stage = 0; if (ScavItemCountScale.Value) Opening.maxItemDropCount = 1;
                orig(self);
            };
            if (Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave")) ProperlySave();
            IL.RoR2.TeleporterInteraction.Start += il =>
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Run>("get_" + nameof(Run.stageClearCountInCurrentLoop)));
                c.Emit(OpCodes.Pop);
                c.EmitDelegate(() => stage);
            };
            TP = LegacyResourcesAPI.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscTeleporter");
            lunarTP = LegacyResourcesAPI.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscLunarTeleporter");
            Stage.onServerStageComplete += _ =>
            {
                TeleporterInteraction tp = FindObjectsOfType<GameObject>().FirstOrDefault(x => x.GetComponent<TeleporterInteraction>() != null)?.GetComponent<TeleporterInteraction>();
                if (tp == null || tp == default) return;
                stage++;
                if (tp.gameObject.name.Contains("LunarTeleporter"))
                {
                    stage = 0;
                    loops++;
                }
                if (ScavItemCountScale.Value) Opening.maxItemDropCount = loops * 5 + stage + 1;
            };
            On.RoR2.SceneDirector.PlaceTeleporter += (orig, self) =>
            {
                if (self.teleporterSpawnCard != null) self.teleporterSpawnCard = (stage == 5 - 1) ? lunarTP : TP;
                orig(self);
            };
            IL.RoR2.Achievements.LoopOnceAchievement.Check += il =>
            {
                // ???
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchLdcI4(0));
                c.Emit(OpCodes.Pop);
                c.EmitDelegate(() => loops);
            };
            CharacterSpawnCard scav = LegacyResourcesAPI.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscScav");
            IL.RoR2.ClassicStageInfo.RebuildCards += il =>
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchStfld<ClassicStageInfo>(nameof(ClassicStageInfo.modifiableMonsterCategories)));
                c.EmitDelegate<Func<DirectorCardCategorySelection, DirectorCardCategorySelection>>(orig =>
                {
                    for (int i = 0; i < orig.categories.Length; i++)
                    {
                        DirectorCardCategorySelection.Category cat = orig.categories[i];
                        for (int j = 0; j < cat.cards.Length; j++)
                        {
                            DirectorCard card = cat.cards[j];
                            if (card.spawnCard is not CharacterSpawnCard) continue;
                            if (card.spawnCard == scav)
                            {
                                card.minimumStageCompletions = ScavOnLevel.Value - 1;
                                card.spawnCard.directorCreditCost = ScavCost.Value;
                            }
                            else if (card.minimumStageCompletions > stage) card.minimumStageCompletions = Mathf.Max(0, card.minimumStageCompletions + (cat.name == "Champions" ? LoopBossesOnLevel.Value : LoopEnemiesOnLevel.Value) - 6);
                            cat.cards[j] = card;
                        }
                        orig.categories[i] = cat;
                    }
                    Log.LogDebug("Enemy Stage Completion\n" + orig.categories.Join(x => x.cards.Join(y => (y.spawnCard?.name ?? "Null") + $" ({y.minimumStageCompletions})"), "\n"));
                    return orig;
                });
            };
            IL.RoR2.DirectorCard.IsAvailable += il =>
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchLdfld<DirectorCard>(nameof(DirectorCard.minimumStageCompletions)));
                c.GotoNext(x => x.MatchClt());
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<DirectorCard, int>>(self => StageCheck(self.minimumStageCompletions + 1) ? 1 : 0);
                c.Emit(OpCodes.Ldc_I4_1);
            };
            IL.RoR2.FamilyDirectorCardCategorySelection.IsAvailable += il =>
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchLdfld<FamilyDirectorCardCategorySelection>(nameof(FamilyDirectorCardCategorySelection.minimumStageCompletion)));
                c.GotoNext(x => x.MatchBgt(out _));
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<FamilyDirectorCardCategorySelection, int>>(self => StageCheck(self.minimumStageCompletion + 1) ? 0 : 2);
                c.Emit(OpCodes.Ldc_I4_1);
                c.GotoNext(x => x.MatchLdfld<FamilyDirectorCardCategorySelection>(nameof(FamilyDirectorCardCategorySelection.maximumStageCompletion)));
                c.GotoNext(x => x.MatchCgt());
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<FamilyDirectorCardCategorySelection, int>>(self => StageCheckMax(self.maximumStageCompletion + 1) ? 0 : 2);
                c.Emit(OpCodes.Ldc_I4_1);
            };
            #region ELITE BS HERE
            Harmony.PatchAll(typeof(PatchLoopGetter));
            /*
            RoR2Application.onLoad += () =>
            {
                if (_thisInitialized) return;
                Log.LogDebug("EliteAPI, amarite?");
                List<string> blacklist = EliteDisables.Value.Split(',').ToList().ConvertAll(x => x.Trim());
                string[] T1s = ["edLightning", "edIce", "edFire", "edEarth"];
                string[] T1Honors = ["edLightningHonor", "edIceHonor", "edFireHonor", "edEarthHonor"];
                string[] T2s = ["edPoison", "edHaunted"];
                float _T1Cost = baseEliteCostMultiplier * PerfectedCost.Value;
                List<EliteTierDef> defaultTiers = new();
                for (var i = 0; i < eliteTiers.Length; i++)
                {
                    EliteTierDef def = eliteTiers[i];
                    if (def.eliteTypes.Any(x => T2s.Contains(x?.name)))
                    {
                        defaultTiers.Add(def);
                        def.costMultiplier *= T2Cost.Value / 6f;
                        def.isAvailable = rules => (T2OnHonor.Value || NotEliteOnlyArtifactActive()) && StageCheck(T2OnLevel.Value, IsEliteOnlyArtifactActive() ? HonorMultiplier.Value : 1);
                    }
                    else if (def.eliteTypes.Any(x => T1Honors.Contains(x?.name)))
                    {
                        def.costMultiplier *= T1Cost.Value;
                        def.isAvailable = rules => IsEliteOnlyArtifactActive() && StageCheck(T1OnLevel.Value, HonorMultiplier.Value);
                    }
                    else if (PerfectedOnLevel.Value > 0 && def.eliteTypes.Any(x => "edLunar" == x?.name))
                    {
                        Log.LogDebug("PerfectedLoop2 in Motion...");
                        def.costMultiplier *= T1Cost.Value;
                        def.isAvailable = rules => rules == SpawnCard.EliteRules.Lunar && StageCheck(PerfectedOnLevel.Value, IsEliteOnlyArtifactActive() ? HonorMultiplier.Value : 1);
                    }
                    else if (def.eliteTypes.Any(x => T1s.Contains(x?.name)))
                    {
                        defaultTiers.Add(def);
                        def.costMultiplier *= T1Cost.Value;
                        _T1Cost = def.costMultiplier;
                        def.isAvailable = rules => NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default && StageCheck(T1OnLevel.Value);
                    }
                    eliteTiers[i] = def;
                }
                defaultTiers.Add(new()
                {
                    eliteTypes = new[] { Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLunar/edLunar.asset").WaitForCompletion() },
                    costMultiplier = _T1Cost,
                    isAvailable = rules => (PerfectedOnHonor.Value || NotEliteOnlyArtifactActive()) && StageCheck(PerfectedOnLevel.Value, IsEliteOnlyArtifactActive() ? HonorMultiplier.Value : 1),
                    canSelectWithoutAvailableEliteDef = false
                });
                List<EliteTierDef> list = eliteTiers.ToList();
                list.RemoveAll(defaultTiers.Contains);
                defaultTiers.Sort((a, b) => (int)Mathf.Sign(a.costMultiplier - b.costMultiplier));
                list.InsertRange(1, defaultTiers);
                eliteTiers = list.ToArray();
                _thisInitialized = true;
                Log.LogDebug("Elites:\n\n" + eliteTiers.Join(x => x.eliteTypes.Join(y => y?.name) + $"({x.costMultiplier})", "\n"));
            };
            IL.RoR2.CombatDirector.PrepareNewMonsterWave += il =>
            {
                ILCursor c = new(il);
                ILLabel label = null;
                c.GotoNext(x => x.MatchLdstr("Card {0} cannot be elite. Skipping elite procedure."));
                c.GotoPrev(x => x.MatchLdsfld(typeof(CombatDirector), nameof(cvDirectorCombatEnableInternalLogs)));
                int branch = c.Index;
                c.GotoPrev(MoveType.After, x => x.MatchLdfld<CharacterSpawnCard>(nameof(CharacterSpawnCard.noElites)), x => x.MatchBrtrue(out _));
                c.MoveAfterLabels();
                c.RemoveRange(branch - c.Index);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<CombatDirector>>(self =>
                {
                    int end = 1;
                    float weight = 0;
                    for (int i = 1; i < eliteTiers.Length; i++)
                    {
                        EliteTierDef eliteTierDef = eliteTiers[i];
                        if (!eliteTierDef.CanSelect(self.currentMonsterCard.spawnCard.eliteRules))
                        {
                            if (cvDirectorCombatEnableInternalLogs.value) 
                                Debug.LogFormat("Elite tier index {0} is unavailable", [i]);
                        }
                        else
                        {
                            float num = self.currentMonsterCard.cost * eliteTierDef.costMultiplier * self.eliteBias;
                            if (num <= self.monsterCredit)
                            {
                                end = i + 1;
                                weight = eliteTierDef.costMultiplier;
                                if (cvDirectorCombatEnableInternalLogs.value)
                                    Debug.LogFormat("Found valid elite tier index {0}", [i]);
                            }
                            else if (cvDirectorCombatEnableInternalLogs.value)
                                Debug.LogFormat("Elite tier index {0} is too expensive ({1}/{2})", [i, num, self.monsterCredit]);
                        }
                    }
                    if (end > 1)
                    {
                        int start = end;
                        EliteSelection.Clear();
                        while (start > 0)
                        {
                            start--;
                            EliteTierDef eliteTierDef = eliteTiers[start];
                            if (cvDirectorCombatEnableInternalLogs.value) Log.LogDebug($"Testing {eliteTierDef.eliteTypes[0]?.name}: {weight} ({eliteTierDef.costMultiplier == weight})");
                            if (eliteTierDef.costMultiplier > weight || !eliteTierDef.CanSelect(self.currentMonsterCard.spawnCard.eliteRules)) break;
                            EliteSelection.AddChoice(eliteTierDef, eliteTierDef.eliteTypes.Count(x => x && x.IsAvailable()));
                        }
                        self.currentActiveEliteTier = EliteSelection.Evaluate(self.rng.nextNormalizedFloat);
                    }
                    else self.currentActiveEliteTier = eliteTiers[0];
                });
                c.GotoNext(x => x.MatchStfld(typeof(CombatDirector), nameof(CombatDirector.currentActiveEliteDef)));
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<CombatDirector, EliteDef>>(self => 
                { 
                    EliteDef def = self.currentActiveEliteTier.GetRandomAvailableEliteDef(self.rng);
                    if (cvDirectorCombatEnableInternalLogs.value)
                    {
                        if (self.currentActiveEliteTier.eliteTypes[0] != null) Log.LogInfo("Current EliteTier: " + self.currentActiveEliteTier.eliteTypes[0].name);
                        if (def != null) Log.LogInfo("Current Elite: " + def.name);
                    }
                    return def;
                });
            };
            */
            #endregion
            On.RoR2.Inventory.SetEquipmentIndexForSlot_EquipmentIndex_uint_uint += (orig, self, equipmentIndex, a, b) =>
            {
                CharacterMaster cm = self.gameObject.GetComponent<CharacterMaster>();
                Log.LogDebug("setting equipment for:" + cm.name);
                if (cm.name == "ArtifactShellMaster" && equipmentIndex == RoR2Content.Equipment.AffixLunar.equipmentIndex) return;
                orig(self, equipmentIndex, a, b);
            };
        }

        [HarmonyPatch]
        public class PatchLoopGetter
        {
            public static bool Prefix(ref int __result)
            {
                __result = loops;
                return false;
            }

            public static MethodBase TargetMethod() => typeof(Run).GetProperty(nameof(Run.loopClearCount)).GetGetMethod();
        }

        public static bool StageCheck(int onLevel, float multiplier = 1)
        {
            if (onLevel == 0) return false;
            if (Chainloader.PluginInfos.ContainsKey("com.TPDespair.ZetArtifacts") && EarlifactActive()) multiplier *= SanctionMultiplier.Value;
            onLevel = Mathf.CeilToInt(onLevel * multiplier) - 1;
            return loops > onLevel / 5 || (loops == onLevel / 5 && stage >= onLevel % 5);
        }
        public static bool StageCheckMax(int onLevel, float multiplier = 1)
        {
            if (onLevel == 0) return false;
            if (Chainloader.PluginInfos.ContainsKey("com.TPDespair.ZetArtifacts") && EarlifactActive()) multiplier *= SanctionMultiplier.Value;
            onLevel = Mathf.CeilToInt(onLevel * multiplier) - 1;
            return loops < onLevel / 5 || (loops == onLevel / 5 && stage < onLevel % 5);
        }

        public static bool EarlifactActive()
        {
            return RunArtifactManager.instance.IsArtifactEnabled(TPDespair.ZetArtifacts.ZetEarlifact.ArtifactDef);
        }

        public static string savePath = System.IO.Path.Combine(Application.persistentDataPath, "ProperSave", "Saves") + "\\" + PluginName + ".csv";
        public static void ProperlySave()
        {
            ProperSave.SaveFile.OnGatherSaveData += _ => save(savePath);
            if (File.Exists(savePath)) J.load();
            void save(string path) => File.WriteAllText(path, $"loops,{loops}\nstage,{stage}");
        }
    }
}
