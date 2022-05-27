using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

namespace HalfLifeCombineRadioChatter
{
    public class GameComponent_WarMusic : GameComponent
    {
        public Dictionary<string, long> lastFiredSounds = new Dictionary<string, long>();
        public Dictionary<Sustainer, long> sustainersByTick = new Dictionary<Sustainer, long>();
        public static GameComponent_WarMusic Instance;
        public GameComponent_WarMusic(Game game)
        {
            Instance = this;
        }
        public static long GetCurrentSeconds
        {
            get
            {
                var dt = DateTime.Now;
                var ticks = dt.Ticks;
                var seconds = ticks / TimeSpan.TicksPerSecond;
                return seconds;
            }
        }

        public bool CanPlaySound(SoundDef soundDef, int secondsCooldown)
        {
            var baseDefName = soundDef.defName.Replace("One", "").Replace("Two", "");
            if (!lastFiredSounds.TryGetValue(baseDefName, out var lastFiredSecond) || GetCurrentSeconds - lastFiredSecond > secondsCooldown)
            {
                return true;
            }
            return false;
        }
        public void AddSound(SoundDef soundDef, float chance, int secondsCooldown, Thing source)
        {
            if (Rand.Chance(chance))
            {
                var baseDefName = soundDef.defName.Replace("One", "").Replace("Two", "");
                if (lastFiredSounds.TryGetValue(baseDefName, out var lastFiredSecond) && GetCurrentSeconds - lastFiredSecond < secondsCooldown)
                {
                    return;
                }
                if (source != null)
                {
                    soundDef.PlayOneShot(source);
                }
                else
                {
                    soundDef.PlayOneShotOnCamera();
                }
                lastFiredSounds.Add(baseDefName, GetCurrentSeconds);
            }
        }
        public void AddSustainer(Sustainer sustainer)
        {
            if (sustainersByTick is null)
            {
                sustainersByTick = new Dictionary<Sustainer, long>();
            }
            sustainersByTick[sustainer] = GetCurrentSeconds + 15;
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            foreach (var key in sustainersByTick.Keys.ToList())
            {
                if (GetCurrentSeconds >= sustainersByTick[key])
                {
                    key.End();
                    sustainersByTick.Remove(key);
                }
            }
        }

        public override void ExposeData()
        {
            Instance = this;
            base.ExposeData();
            Scribe_Collections.Look(ref lastFiredSounds, "lastFiredSounds", LookMode.Value, LookMode.Value);
        }
    }
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
                if (GameComponent_WarMusic.Instance.CanPlaySound(HL_DefOf.HLCRC_SelectPawnOne, 3))
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
                GameComponent_WarMusic.Instance.AddSound(def, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                    var def = Rand.Bool ? HL_DefOf.HLCRC_AttackOne : HL_DefOf.HLCRC_AttackTwo;
                    GameComponent_WarMusic.Instance.AddSound(def, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                    var def = Rand.Bool ? HL_DefOf.HLCRC_AttackOne : HL_DefOf.HLCRC_AttackTwo;
                    GameComponent_WarMusic.Instance.AddSound(def, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                var def = Rand.Bool ? HL_DefOf.HLCRC_MovingOne : HL_DefOf.HLCRC_MovingTwo;
                GameComponent_WarMusic.Instance.AddSound(def, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                    if (dinfo.Instigator != null && dinfo.Instigator.Faction == pawn.Faction)
                    {
                        return;
                    }
                    var def = Rand.Bool ? HL_DefOf.HLCRC_PawnIsHurtOne : HL_DefOf.HLCRC_PawnIsHurtTwo;
                    GameComponent_WarMusic.Instance.AddSound(def, 0.2f, Rand.RangeInclusive(2, 4), pawn);
                }
                else if (dinfo.Instigator is Pawn attacker && attacker.IsColonist)
                {
                    var def = Rand.Bool ? HL_DefOf.HLCRC_HittingOrDowningEnemyOne : HL_DefOf.HLCRC_HittingOrDowningEnemyTwo;
                    GameComponent_WarMusic.Instance.AddSound(def, 0.2f, Rand.RangeInclusive(1, 3), attacker);
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
                var def = Rand.Bool ? HL_DefOf.HLCRC_ColonistDeathOne : HL_DefOf.HLCRC_ColonistDeathTwo;
                def.PlayOneShot(new TargetInfo(__instance.PositionHeld, __instance.MapHeld));
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
    //            var def = Rand.Bool ? HL_DefOf.HLCRC_PawnGetsInfectionOne : HL_DefOf.HLCRC_PawnGetsInfectionTwo;
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
                HL_DefOf.HLCRC_FormCaravan.PlayOneShotOnCamera();
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class IncidentWorker_Raid_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            if (parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer) && parms.target is Map map)
            {
                var comp = GameComponent_WarMusic.Instance;
                var worker = parms.raidArrivalMode.Worker;
                if (worker is PawnsArrivalModeWorker_CenterDrop || worker is PawnsArrivalModeWorker_EdgeDrop 
                    || worker is PawnsArrivalModeWorker_EdgeDropGroups || worker is PawnsArrivalModeWorker_RandomDrop)
                {
                    var sus = HL_DefOf.HLCRC_WarMusicLoopDropPods.TrySpawnSustainer(SoundInfo.OnCamera());
                    comp.AddSustainer(sus);
                }
                else
                {
                    if (parms.faction.def.humanlikeFaction)
                    {
                        HL_DefOf.HLCRC_HumanRaidEvent.PlayOneShotOnCamera();
                    }
                    else if (parms.faction == Faction.OfMechanoids)
                    {
                        var sus = HL_DefOf.HLCRC_WarMusicLoopMechanoidRaid.TrySpawnSustainer(SoundInfo.OnCamera());
                        comp.AddSustainer(sus);
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
            if (__result && parms.target is Map map)
            {
                if (__instance.def == HL_DefOf.ProblemCauser)
                {
                    var def = HL_DefOf.HLCRC_WarMusicLoopHumanRaid;
                    var sus = def.TrySpawnSustainer(SoundInfo.OnCamera());
                    GameComponent_WarMusic.Instance.AddSustainer(sus);
                }
                else if (__instance.def == HL_DefOf.DefoliatorShipPartCrash)
                {
                    var def = HL_DefOf.HLCRC_WarMusicLoopInfestation;
                    var sus = def.TrySpawnSustainer(SoundInfo.OnCamera());
                    GameComponent_WarMusic.Instance.AddSustainer(sus);
                }
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Infestation), "TryExecuteWorker")]
    public static class IncidentWorker_Infestation_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            if (parms.target is Map map)
            {
                HL_DefOf.HLCRC_InfestationEvent.PlayOneShotOnCamera();
            }
        }
    }

    [HarmonyPatch(typeof(MechClusterUtility), "SpawnCluster")]
    public static class MechClusterUtility_SpawnCluster_Patch
    {
        public static void Postfix(IntVec3 center, Map map, MechClusterSketch sketch, bool dropInPods = true, 
            bool canAssaultColony = false, string questTag = null)
        {
            var sus = HL_DefOf.HLCRC_WarMusicLoopMechanoidRaid.TrySpawnSustainer(SoundInfo.OnCamera());
            GameComponent_WarMusic.Instance.AddSustainer(sus);
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

    //[HarmonyPatch(typeof(ShortCircuitUtility), "DoShortCircuit")]
    //public static class ShortCircuitUtility_DoShortCircuit_Patch
    //{
    //    public static void Postfix()
    //    {
    //        HL_DefOf.HLCRC_Zzzt.PlayOneShotOnCamera();
    //    }
    //}
    //

    [HarmonyPatch(typeof(ManhunterPackIncidentUtility), "GenerateAnimals")]
    public static class ManhunterPackIncidentUtility_GenerateAnimals_Patch
    {
        public static void Postfix(PawnKindDef animalKind, int tile, float points, int animalCount = 0)
        {
            var mapParent = Find.World.worldObjects.ObjectsAt(tile).OfType<MapParent>().FirstOrDefault(x => x.Map != null);
            if (mapParent != null)
            {
                var sus = HL_DefOf.HLCRC_WarMusicManhunter.TrySpawnSustainer(SoundInfo.OnCamera());
                var comp = GameComponent_WarMusic.Instance;
                comp.AddSustainer(sus);
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
                HL_DefOf.HLCRC_SolarFlare.PlayOneShotOnCamera();
            }
            else if (cond.def == GameConditionDefOf.ToxicFallout)
            {
                HL_DefOf.HLCRC_ToxicFallout.PlayOneShotOnCamera();
            }
            else if (cond.def == GameConditionDefOf.ToxicFallout)
            {
                HL_DefOf.HLCRC_ToxicSpewer.PlayOneShotOnCamera();
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
}
