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
        public bool CanPlaySound(string defName, Pawn pawn, int secondsCooldown)
        {
            var def = GetBaseSoundDefFor(defName, pawn);
            if (def != null)
            {
                return CanPlaySound(def, secondsCooldown);
            }
            return false;
        }

        public bool CanPlaySound(SoundDef soundDef, int secondsCooldown)
        {
            var baseDefName = soundDef.defName.Replace("One", "").Replace("Two", "");
            if (lastFiredSounds is null)
            {
                lastFiredSounds = new Dictionary<string, long>();
            }
            if (!lastFiredSounds.TryGetValue(baseDefName, out var lastFiredSecond) || GetCurrentSeconds - lastFiredSecond > secondsCooldown)
            {
                return true;
            }
            return false;
        }

        public SoundDef GetBaseSoundDefFor(string defName, Pawn pawn)
        {
            var combinedDefName = HalfLifeCombineRadioChatterMod.Prefix + HalfLifeCombineRadioChatterMod.GetVoiceFor(pawn) + defName + (Rand.Bool ? "One" : "Two");
            return DefDatabase<SoundDef>.GetNamedSilentFail(combinedDefName);
        }

        public void AddSound(string defName, Pawn pawn, float chance, int secondsCooldown, Thing source)
        {
            var def = GetBaseSoundDefFor(defName, pawn);
            if (def != null)
            {
                AddSound(defName, def, chance, secondsCooldown, source);
            }
        }
        private void AddSound(string baseDefName, SoundDef soundDef, float chance, int secondsCooldown, Thing source)
        {
            if (Rand.Chance(chance))
            {
                if (lastFiredSounds is null)
                {
                    lastFiredSounds = new Dictionary<string, long>();
                }
                if (lastFiredSounds.TryGetValue(baseDefName, out var lastFiredSecond) && GetCurrentSeconds - lastFiredSecond < secondsCooldown)
                {
                    return;
                }
                if (source != null)
                {
                    soundDef.PlayOneShot(new TargetInfo(source.PositionHeld, source.MapHeld));
                }
                else
                {
                    soundDef.PlayOneShotOnCamera();
                }
                lastFiredSounds[baseDefName] = GetCurrentSeconds;
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
            if (sustainersByTick != null)
            {
                foreach (var key in sustainersByTick.Keys.ToList())
                {
                    if (GetCurrentSeconds >= sustainersByTick[key])
                    {
                        key.End();
                        sustainersByTick.Remove(key);
                    }
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

    [StaticConstructorOnStartup]
    public static class HalfLifeCombineRadioChatterStartup
    {
        static HalfLifeCombineRadioChatterStartup()
        {
            ApplySettings();
        }

        public static void ApplySettings()
        {
            var sounds = DefDatabase<SoundDef>.AllDefsListForReading.ListFullCopy();
            foreach (var sound in sounds)
            {
                foreach (var disabledActor in HalfLifeCombineRadioChatterSettings.disabledVoiceActors)
                {
                    if (sound.defName.StartsWith(HalfLifeCombineRadioChatterMod.Prefix + disabledActor))
                    {
                        DefDatabase<SoundDef>.Remove(sound);
                    }
                }
            }
        }
    }
    public class HalfLifeCombineRadioChatterMod : Mod
    {
        public const string Prefix = "HLCRC_";

        public static List<string> voiceActors = new List<string>
        {
            "Female1",
            "Female2",
            "Male1",
            "Male2"
        };

        public static string GetVoiceFor(Pawn pawn)
        {
            var gender = pawn.gender == Gender.Male ? "Male" : "Female";
            var voicePool = voiceActors.Where(x => x.StartsWith(gender)).ToList();
            var availableVoices = new List<string>();
            foreach (var voiceActor in voicePool)
            {
                if (!HalfLifeCombineRadioChatterSettings.disabledVoiceActors.Contains(voiceActor))
                {
                    availableVoices.Add(voiceActor);
                }
            }
            Rand.PushState(pawn.thingIDNumber);
            if (availableVoices.TryRandomElement(out var voice))
            {
                Rand.PopState();
                return voice;
            }
            Rand.PopState();
            return "";
        }

        public static HalfLifeCombineRadioChatterSettings settings;
        public HalfLifeCombineRadioChatterMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<HalfLifeCombineRadioChatterSettings>();
            new Harmony("HalfLifeCombineRadioChatter.Mod").PatchAll();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            HalfLifeCombineRadioChatterStartup.ApplySettings();
        }
    }

    public class HalfLifeCombineRadioChatterSettings : ModSettings
    {

        public static List<string> disabledVoiceActors = new List<string>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref disabledVoiceActors, "disabledVoiceActors");
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.Label("HLCRC.EnableDisableVoiceActors".Translate());
            foreach (var actor in HalfLifeCombineRadioChatterMod.voiceActors)
            {
                var enabled = disabledVoiceActors.Contains(actor) is true;
                listingStandard.CheckboxLabeled(actor, ref enabled);
                if (enabled)
                {
                    if (disabledVoiceActors.Contains(actor))
                    {
                        disabledVoiceActors.Remove(actor);
                    }
                }
                else
                {
                    if (!disabledVoiceActors.Contains(actor))
                    {
                        disabledVoiceActors.Add(actor);
                    }
                }
            }
            listingStandard.End();
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
                if (GameComponent_WarMusic.Instance.CanPlaySound("SelectPawn", pawn, 3))
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
                GameComponent_WarMusic.Instance.AddSound("SelectPawn", pawn, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                    GameComponent_WarMusic.Instance.AddSound("Attack", pawn, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                    GameComponent_WarMusic.Instance.AddSound("Attack", pawn, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                GameComponent_WarMusic.Instance.AddSound("Moving", pawn, 1f, Rand.RangeInclusive(1, 3), pawn);
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
                    GameComponent_WarMusic.Instance.AddSound("PawnIsDowned", ___pawn, 1f, 0, ___pawn);
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
                    //var def = Rand.Bool ? HL_DefOf.HLCRC_PawnIsHurtOne : HL_DefOf.HLCRC_PawnIsHurtTwo;
                    //GameComponent_WarMusic.Instance.AddSound(def, 0.2f, Rand.RangeInclusive(2, 4), pawn);
                }
                else if (dinfo.Instigator is Pawn attacker && attacker.IsColonist)
                {
                    GameComponent_WarMusic.Instance.AddSound("HittingOrDowningEnemy", attacker, 1f, Rand.RangeInclusive(5, 7), attacker);
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
                GameComponent_WarMusic.Instance.AddSound("ColonistDeath", __instance, 1f, 0, __instance);
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
