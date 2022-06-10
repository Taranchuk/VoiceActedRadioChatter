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

namespace VoiceActedRadioChatter
{

    [StaticConstructorOnStartup]
    public static class VoiceActedRadioChatterStartup
    {
        public static List<SoundDef> eventSounds = new List<SoundDef>
        {
            VARC_DefOf.VARC_FormCaravan,
            VARC_DefOf.VARC_MentalBreak,
            VARC_DefOf.VARC_TransportCrash,
            VARC_DefOf.VARC_Zzzt,
            VARC_DefOf.VARC_SolarFlare,
            VARC_DefOf.VARC_Arrested,
            VARC_DefOf.VARC_Betrayal,
            VARC_DefOf.VARC_HumanRaidEvent,
            VARC_DefOf.VARC_InfestationEvent,
            VARC_DefOf.VARC_WarMusicManhunter,
            VARC_DefOf.VARC_ToxicSpewer,
            VARC_DefOf.VARC_ToxicFallout,
            VARC_DefOf.VARC_WarMusicLoopDropPods,
            VARC_DefOf.VARC_WarMusicLoopHumanRaid,
            VARC_DefOf.VARC_WarMusicLoopInfestation,
            VARC_DefOf.VARC_WarMusicLoopMechanoidRaid,
        };
        static VoiceActedRadioChatterStartup()
        {
            ApplySettings();
        }

        public static void ApplySettings()
        {
            var sounds = DefDatabase<SoundDef>.AllDefsListForReading.ListFullCopy();
            foreach (var sound in sounds)
            {
                foreach (var disabledActor in VoiceActedRadioChatterSettings.disabledVoiceActors)
                {
                    if (sound.defName.StartsWith(VoiceActedRadioChatterMod.Prefix + disabledActor))
                    {
                        DefDatabase<SoundDef>.Remove(sound);
                    }
                }
            }

            if (VoiceActedRadioChatterSettings.cooldownSecondsBySounds is null)
            {
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds = new Dictionary<string, CooldownValue>();
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds[VoiceActedRadioChatterMod.SelectPawn] = new CooldownValue
                {
                    min = 1,
                    max = 3
                };
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds[VoiceActedRadioChatterMod.Attack] = new CooldownValue
                {
                    min = 1,
                    max = 3
                };
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds[VoiceActedRadioChatterMod.Moving] = new CooldownValue
                {
                    min = 1,
                    max = 3
                };
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds[VoiceActedRadioChatterMod.PawnIsDowned] = new CooldownValue
                {
                    min = 0,
                    max = 0
                };
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds[VoiceActedRadioChatterMod.ColonistDeath] = new CooldownValue
                {
                    min = 0,
                    max = 0
                };
                VoiceActedRadioChatterSettings.cooldownSecondsBySounds[VoiceActedRadioChatterMod.HittingOrDowningEnemy] = new CooldownValue
                {
                    min = 5,
                    max = 7
                };
            }
        }
    }
    public class VoiceActedRadioChatterMod : Mod
    {
        public const string Prefix = "VARC_";

        public static List<string> voiceActors = new List<string>
        {
            "Female1",
            "Female2",
            "Female3",
            "Female4",
            "Female5",
            "Female6",
            "Female7",
            "Female8",
            "Female9",
            "Female10",
            "Female11",
            "Female12",
            "Female13",
            "Female14",
            "Male1",
            "Male2",
            "Male3",
            "Male4",
            "Male5",
            "Male6",
            "Male7",
            "Male8",
            "Male9",
            "Male10",
            "Male11",
            "Male12",
            "Male13",
            "Male14",
            "Male15",
            "Male16",
            "Male17",
        };

        public static List<string> unitResponses = new List<string>
        {
            SelectPawn,
            Attack,
            Moving,
            PawnIsDowned,
            HittingOrDowningEnemy,
            ColonistDeath
        };

        public const string SelectPawn = "SelectPawn";
        public const string Attack = "Attack";
        public const string Moving = "Moving";
        public const string PawnIsDowned = "PawnIsDowned";
        public const string HittingOrDowningEnemy = "HittingOrDowningEnemy";
        public const string ColonistDeath = "ColonistDeath";
        public static int GetCooldownSecondsFor(string sound)
        {
            return VoiceActedRadioChatterSettings.cooldownSecondsBySounds[sound].GetValue;
        }

        public static string GetVoiceFor(Pawn pawn)
        {
            if (VoiceActedRadioChatterSettings.fixedVoicesForPawns.TryGetValue(pawn.Name.ToStringFull, out var voice))
            {
                return voice;
            }
            var voicePool = GetVoicePool(pawn);
            Rand.PushState(pawn.thingIDNumber);
            if (voicePool.TryRandomElement(out voice))
            {
                Rand.PopState();
                return voice;
            }
            Rand.PopState();
            return "";
        }

        public static List<string> GetVoicePool(Pawn pawn)
        {
            var gender = pawn.gender == Gender.Male ? "Male" : "Female";
            return voiceActors.Where(x => x.StartsWith(gender) && !VoiceActedRadioChatterSettings.disabledVoiceActors.Contains(x)).ToList();
        }

        public static VoiceActedRadioChatterSettings settings;
        public VoiceActedRadioChatterMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<VoiceActedRadioChatterSettings>();
            new Harmony("VoiceActedRadioChatter.Mod").PatchAll();
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
            VoiceActedRadioChatterStartup.ApplySettings();
        }
    }
}
