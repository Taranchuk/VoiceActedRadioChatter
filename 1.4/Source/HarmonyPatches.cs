using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VoiceActedRadioChatter
{
    [HarmonyPatch(typeof(Selector), "SelectInternal")]
    public static class Selector_SelectInternal_Patch
    {
        public static int prevFrame;
        public static void Prefix(out bool __state, object obj, ref bool playSound)
        {
            __state = false;
            if (prevFrame != Time.frameCount && obj is Pawn pawn && pawn.IsColonist)
            {
                if (GameComponent_WarMusic.Instance.CanPlaySound("SelectPawn", pawn))
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
                Pawn pawn = obj as Pawn;
                GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.SelectPawn, pawn, 1f, pawn);
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
                Action storedAction = __result;
                __result = delegate
                {
                    GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.Attack, pawn, 1f, pawn);
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
                Action storedAction = __result;
                __result = delegate
                {
                    GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.Attack, pawn, 1f, pawn);
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
                GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.Moving, pawn, 1f, pawn);
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
                    GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.PawnIsDowned, ___pawn, 1f, ___pawn);
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
                    //if (dinfo.Instigator != null && dinfo.Instigator.Faction == pawn.Faction)
                    //{
                    //    return;
                    //}
                    //var def = Rand.Bool ? HL_DefOf.VARC_PawnIsHurtOne : HL_DefOf.VARC_PawnIsHurtTwo;
                    //GameComponent_WarMusic.Instance.AddSound(def, 0.2f, Rand.RangeInclusive(2, 4), pawn);
                }
                else if (dinfo.Instigator is Pawn attacker && attacker.IsColonist)
                {
                    GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.HittingOrDowningEnemy, attacker, 1f, attacker);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        private static void Postfix(Pawn __instance)
        {
            if (__instance.Dead && __instance.IsColonist)
            {
                GameComponent_WarMusic.Instance.AddSound(VoiceActedRadioChatterMod.ColonistDeath, __instance, 1f, __instance);
            }
        }
    }

    //[HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
    //{
    //    typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)
    //})]
    //public static class Pawn_HealthTracker_AddHediff_Patch
    //{
    //    private static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn, Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
    //    {
    //        if (___pawn.IsColonist && hediff.TryGetComp<HediffComp_Immunizable>() != null && hediff.def.lethalSeverity >= 1f)
    //        {
    //            var def = Rand.Bool ? HL_DefOf.VARC_PawnGetsInfectionOne : HL_DefOf.VARC_PawnGetsInfectionTwo;
    //            def.PlayOneShot(___pawn);
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(Dialog_FormCaravan), "TryFormAndSendCaravan")]
    public static class Dialog_FormCaravan_TryFormAndSendCaravan_Patch
    {
        public static void Postfix(Dialog_FormCaravan __instance, bool __result)
        {
            if (__result)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_FormCaravan.defName))
                {
                    VARC_DefOf.VARC_FormCaravan.PlayOneShotOnCamera();
                }
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class IncidentWorker_Raid_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            if (parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer) && parms.target is Map)
            {
                GameComponent_WarMusic comp = GameComponent_WarMusic.Instance;
                PawnsArrivalModeWorker worker = parms.raidArrivalMode.Worker;
                if (worker is PawnsArrivalModeWorker_CenterDrop || worker is PawnsArrivalModeWorker_EdgeDrop
                    || worker is PawnsArrivalModeWorker_EdgeDropGroups || worker is PawnsArrivalModeWorker_RandomDrop)
                {
                    if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_WarMusicLoopDropPods.defName))
                    {
                        Sustainer sus = VARC_DefOf.VARC_WarMusicLoopDropPods.TrySpawnSustainer(SoundInfo.OnCamera());
                        comp.AddSustainer(sus);
                    }
                }
                else
                {
                    if (parms.faction.def.humanlikeFaction)
                    {
                        if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_HumanRaidEvent.defName))
                        {
                            VARC_DefOf.VARC_HumanRaidEvent.PlayOneShotOnCamera();
                        }
                    }
                    else if (parms.faction == Faction.OfMechanoids)
                    {
                        if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_WarMusicLoopMechanoidRaid.defName))
                        {
                            Sustainer sus = VARC_DefOf.VARC_WarMusicLoopMechanoidRaid.TrySpawnSustainer(SoundInfo.OnCamera());
                            comp.AddSustainer(sus);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public class IncidentWorker_TryExecute
    {
        private static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (__result && parms.target is Map)
            {
                if (__instance.def == VARC_DefOf.ProblemCauser)
                {
                    if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_WarMusicLoopHumanRaid.defName))
                    {
                        SoundDef def = VARC_DefOf.VARC_WarMusicLoopHumanRaid;
                        Sustainer sus = def.TrySpawnSustainer(SoundInfo.OnCamera());
                        GameComponent_WarMusic.Instance.AddSustainer(sus);
                    }
                }
                else if (__instance.def == VARC_DefOf.DefoliatorShipPartCrash)
                {
                    if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_WarMusicLoopInfestation.defName))
                    {
                        SoundDef def = VARC_DefOf.VARC_WarMusicLoopInfestation;
                        Sustainer sus = def.TrySpawnSustainer(SoundInfo.OnCamera());
                        GameComponent_WarMusic.Instance.AddSustainer(sus);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Infestation), "TryExecuteWorker")]
    public static class IncidentWorker_Infestation_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            if (parms.target is Map)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_InfestationEvent.defName))
                {
                    VARC_DefOf.VARC_InfestationEvent.PlayOneShotOnCamera();
                }
            }
        }
    }

    [HarmonyPatch(typeof(MechClusterUtility), "SpawnCluster")]
    public static class MechClusterUtility_SpawnCluster_Patch
    {
        public static void Postfix(IntVec3 center, Map map, MechClusterSketch sketch, bool dropInPods = true,
            bool canAssaultColony = false, string questTag = null)
        {
            if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_WarMusicLoopMechanoidRaid.defName))
            {
                Sustainer sus = VARC_DefOf.VARC_WarMusicLoopMechanoidRaid.TrySpawnSustainer(SoundInfo.OnCamera());
                GameComponent_WarMusic.Instance.AddSustainer(sus);
            }
        }
    }

    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public class MentalStateHandler_TryStartMentalState_Patch
    {
        private static void Postfix(MentalStateHandler __instance, Pawn ___pawn, bool __result, MentalStateDef stateDef, string reason = null, bool forceWake = false, bool causedByMood = false, Pawn otherPawn = null, bool transitionSilently = false)
        {
            if (__result && ___pawn.IsColonist && ___pawn.DevelopmentalStage == DevelopmentalStage.Adult)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_MentalBreak.defName))
                {
                    VARC_DefOf.VARC_MentalBreak.PlayOneShotOnCamera();
                }
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
                    if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_TransportCrash.defName))
                    {
                        VARC_DefOf.VARC_TransportCrash.PlayOneShotOnCamera();
                    }
                }
            }
        }
    }

    //[HarmonyPatch(typeof(ShortCircuitUtility), "DoShortCircuit")]
    //public static class ShortCircuitUtility_DoShortCircuit_Patch
    //{
    //    public static void Postfix()
    //    {
    //        HL_DefOf.VARC_Zzzt.PlayOneShotOnCamera();
    //    }
    //}
    //

    [HarmonyPatch(typeof(ManhunterPackIncidentUtility), "GenerateAnimals")]
    public static class ManhunterPackIncidentUtility_GenerateAnimals_Patch
    {
        public static void Postfix(PawnKindDef animalKind, int tile, float points, int animalCount = 0)
        {
            MapParent mapParent = Find.World.worldObjects.ObjectsAt(tile).OfType<MapParent>().FirstOrDefault(x => x.Map != null);
            if (mapParent != null)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_WarMusicManhunter.defName))
                {
                    Sustainer sus = VARC_DefOf.VARC_WarMusicManhunter.TrySpawnSustainer(SoundInfo.OnCamera());
                    GameComponent_WarMusic comp = GameComponent_WarMusic.Instance;
                    comp.AddSustainer(sus);
                }
            }
        }
    }


    [HarmonyPatch(typeof(GameConditionManager), "RegisterCondition")]
    public class GameConditionManager_RegisterCondition_Patch
    {
        public static void Postfix(GameCondition cond)
        {
            if (cond.def == GameConditionDefOf.SolarFlare)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_SolarFlare.defName))
                {
                    VARC_DefOf.VARC_SolarFlare.PlayOneShotOnCamera();
                }
            }
            else if (cond.def == GameConditionDefOf.ToxicFallout)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_ToxicFallout.defName))
                {
                    VARC_DefOf.VARC_ToxicFallout.PlayOneShotOnCamera();
                }
            }
            else if (cond.def == GameConditionDefOf.ToxicFallout)
            {
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_ToxicSpewer.defName))
                {
                    VARC_DefOf.VARC_ToxicSpewer.PlayOneShotOnCamera();
                }
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
                if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_Arrested.defName))
                {
                    VARC_DefOf.VARC_Arrested.PlayOneShotOnCamera();
                }
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
            if (!VoiceActedRadioChatterSettings.disabledEventSounds.Contains(VARC_DefOf.VARC_Betrayal.defName))
            {
                VARC_DefOf.VARC_Betrayal.PlayOneShotOnCamera();
            }
        }
    }
}
