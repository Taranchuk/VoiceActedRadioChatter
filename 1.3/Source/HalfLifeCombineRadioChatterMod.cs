using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HalfLifeCombineRadioChatter
{
    public class HalfLifeCombineRadioChatterMod : Mod
    {
        public HalfLifeCombineRadioChatterMod(ModContentPack pack) : base(pack)
        {
            new Harmony("HalfLifeCombineRadioChatter.Mod").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Selector), "SelectInternal")]
    public static class Selector_SelectInternal_Patch
    {
        public static int prevFrame;
        public static void Prefix(out bool __state, object obj, ref bool playSound)
        {
            __state = false;
            if (prevFrame != Time.frameCount && obj is Pawn pawn && pawn.IsColonist)
            {
                if (Rand.Chance(0.2f))
                {
                    playSound = false;
                    __state = true;
                }
            }
            prevFrame = Time.frameCount;
        }
        public static void Postfix(bool __state, object obj)
        {
            if (__state)
            {
                var pawn = obj as Pawn;
                var def = Rand.Bool ? HL_DefOf.HLCRC_SelectPawnOne : HL_DefOf.HLCRC_SelectPawnTwo;
                def.PlayOneShot(pawn);
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuUtility), "GetRangedAttackAction")]
    public static class FloatMenuUtility_GetRangedAttackAction_Patch
    {
        public static int prevFrame;
        public static void Postfix(Pawn pawn, ref Action __result)
        {
            if (prevFrame != Time.frameCount && __result != null && pawn.IsColonist)
            {
                var storedAction = __result;
                __result = delegate
                {
                    if (Rand.Chance(0.45f))
                    {
                        var def = Rand.Bool ? HL_DefOf.HLCRC_AttackOne : HL_DefOf.HLCRC_AttackTwo;
                        def.PlayOneShot(pawn);
                    }
                    storedAction();
                };
            }
            prevFrame = Time.frameCount;
        }
    }

    [HarmonyPatch(typeof(FloatMenuUtility), "GetMeleeAttackAction")]
    public static class FloatMenuUtility_GetMeleeAttackAction_Patch
    {
        public static int prevFrame;
        public static void Postfix(Pawn pawn, ref Action __result)
        {
            if (prevFrame != Time.frameCount && __result != null && pawn.IsColonist)
            {
                var storedAction = __result;
                __result = delegate
                {
                    if (Rand.Chance(0.45f))
                    {
                        var def = Rand.Bool ? HL_DefOf.HLCRC_AttackOne : HL_DefOf.HLCRC_AttackTwo;
                        def.PlayOneShot(pawn);
                    }
                    storedAction();
                };
            }
            prevFrame = Time.frameCount;
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "PawnGotoAction")]
    public static class FloatMenuMakerMap_PawnGotoAction_Patch
    {
        public static int prevFrame;
        public static void Postfix(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
        {
            if (prevFrame != Time.frameCount && pawn.IsColonist)
            {
                if (Rand.Chance(0.2f))
                {
                    var def = Rand.Bool ? HL_DefOf.HLCRC_MovingOne : HL_DefOf.HLCRC_MovingTwo;
                    def.PlayOneShot(pawn);
                }
            }
            prevFrame = Time.frameCount;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Pawn_HealthTracker_MakeDowned_Patch
    {
        private static void Postfix(Pawn ___pawn, DamageInfo? dinfo, Hediff hediff)
        {
            if (___pawn.IsColonist && ___pawn.Downed)
            {
                if (Rand.Chance(0.2f))
                {
                    var def = Rand.Bool ? HL_DefOf.HLCRC_PawnIsDownedOne : HL_DefOf.HLCRC_PawnIsDownedTwo;
                    def.PlayOneShot(___pawn);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Thing_TakeDamage_Patch
    {
        public static void Postfix(Thing __instance, DamageInfo dinfo)
        {
            if (__instance is Pawn pawn)
            {
                if (pawn.IsColonist)
                {
                    if (Rand.Chance(0.2f))
                    {
                        var def = Rand.Bool ? HL_DefOf.HLCRC_PawnIsHurtOne : HL_DefOf.HLCRC_PawnIsHurtTwo;
                        def.PlayOneShot(pawn);
                    }
                }
                else if (dinfo.Instigator is Pawn attacker && attacker.IsColonist)
                {
                    if (Rand.Chance(0.2f))
                    {
                        var def = Rand.Bool ? HL_DefOf.HLCRC_HittingOrDowningEnemyOne : HL_DefOf.HLCRC_HittingOrDowningEnemyTwo;
                        def.PlayOneShot(attacker);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
    {
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)
    })]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        private static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn, Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            if (___pawn.IsColonist && hediff.TryGetComp<HediffComp_Immunizable>() != null && hediff.def.lethalSeverity >= 1f)
            {
                var def = Rand.Bool ? HL_DefOf.HLCRC_PawnGetsInfectionOne : HL_DefOf.HLCRC_PawnGetsInfectionTwo;
                def.PlayOneShot(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_FormCaravan), "TryFormAndSendCaravan")]
    public static class Dialog_FormCaravan_TryFormAndSendCaravan_Patch
    {
        public static void Postfix(Dialog_FormCaravan __instance, bool __result)
        {
            if (__result)
            {
                HL_DefOf.HLCRC_FormCaravan.PlayOneShotOnCamera();
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class IncidentWorker_Raid_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            if (parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer))
            {
                var worker = parms.raidArrivalMode.Worker;
                if (worker is PawnsArrivalModeWorker_CenterDrop || worker is PawnsArrivalModeWorker_EdgeDrop 
                    || worker is PawnsArrivalModeWorker_EdgeDropGroups || worker is PawnsArrivalModeWorker_RandomDrop)
                {
                    var sus = HL_DefOf.HLCRC_WarMusicLoopDropPods.TrySpawnSustainer(SoundInfo.OnCamera());
                    sus.endRealTime = Time.realtimeSinceStartup + 3;
                }
                else
                {
                    if (parms.faction.def.humanlikeFaction)
                    {
                        var sus = HL_DefOf.HLCRC_WarMusicLoopHumanRaid.TrySpawnSustainer(SoundInfo.OnCamera());
                        sus.endRealTime = Time.realtimeSinceStartup + 3;
                    }
                    else if (parms.faction == Faction.OfMechanoids)
                    {
                        HL_DefOf.HLCRC_WarMusicLoopMechanoidRaid.TrySpawnSustainer(SoundInfo.OnCamera());
                    }
                }
            }
        }
    }


    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public class MentalStateHandler_TryStartMentalState_Patch
    {
        private static void Postfix(MentalStateHandler __instance, Pawn ___pawn, bool __result, MentalStateDef stateDef, string reason = null, bool forceWake = false, bool causedByMood = false, Pawn otherPawn = null, bool transitionSilently = false)
        {
            if (__result && ___pawn.IsColonist)
            {
                HL_DefOf.HLCRC_MentalBreak.PlayOneShotOnCamera();
            }
        }
    }

    [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Prefix(ref Thing newThing, ref WipeMode wipeMode, bool respawningAfterLoad)
        {
            if (newThing is Pawn pawn)
            {
                if (Find.QuestManager.QuestsListForReading.Any(x => x.root == QuestScriptDefOf.RefugeePodCrash 
                    && x.QuestLookTargets.Contains(pawn)))
                {
                    HL_DefOf.HLCRC_TransportCrash.PlayOneShotOnCamera();
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShortCircuitUtility), "DoShortCircuit")]
    public static class ShortCircuitUtility_DoShortCircuit_Patch
    {
        public static void Postfix()
        {
            HL_DefOf.HLCRC_Zzzt.PlayOneShotOnCamera();
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Infestation), "TryExecuteWorker")]
    public static class IncidentWorker_Infestation_TryExecuteWorker_Patch
    {
        public static void Postfix()
        {
            HL_DefOf.HLCRC_WarMusicLoopInfestation.TrySpawnSustainer(SoundInfo.OnCamera());
        }
    }

    [HarmonyPatch(typeof(ManhunterPackIncidentUtility), "GenerateAnimals")]
    public static class ManhunterPackIncidentUtility_GenerateAnimals_Patch
    {
        public static void Postfix()
        {
            var sus = HL_DefOf.HLCRC_WarMusicManhunter.TrySpawnSustainer(SoundInfo.OnCamera());
            sus.endRealTime = Time.realtimeSinceStartup + 30;
        }
    }


    [HarmonyPatch(typeof(GameConditionManager), "RegisterCondition")]
    public class GameConditionManager_RegisterCondition_Patch
    {
        public static void Postfix(GameCondition cond)
        {
            if (cond.def == GameConditionDefOf.SolarFlare)
            {
                HL_DefOf.HLCRC_SolarFlare.PlayOneShotOnCamera();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "CheckAcceptArrest")]
    public class Pawn_CheckAcceptArrest_Patch
    {
        public static void Postfix(bool __result)
        {
            if (__result)
            {
                HL_DefOf.HLCRC_Arrested.PlayOneShotOnCamera();
            }
        }
    }

    [HarmonyPatch(typeof(PrisonBreakUtility), "StartPrisonBreak",
        new Type[] { typeof(Pawn), typeof(string), typeof(string), typeof(LetterDef) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out })]
    public static class Patch_StartPrisonBreak
    {
        public static void Postfix(Pawn initiator, ref string letterText, ref string letterLabel, ref LetterDef letterDef)
        {
            HL_DefOf.HLCRC_Betrayal.PlayOneShotOnCamera();
        }
    }

    [HarmonyPatch(typeof(SubSustainer), "StartSample")]
    public static class Patch_SubSustainer_Patch
    {
        public static void Postfix(SubSustainer __instance)
        {
            Log.Message("Starting " + __instance.parent?.def);
        }
    }
}
