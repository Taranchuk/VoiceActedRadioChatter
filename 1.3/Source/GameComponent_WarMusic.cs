using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace VoiceActedRadioChatter
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
            var combinedDefName = VoiceActedRadioChatterMod.Prefix + VoiceActedRadioChatterMod.GetVoiceFor(pawn) + defName + (Rand.Bool ? "One" : "Two");
            return DefDatabase<SoundDef>.GetNamedSilentFail(combinedDefName);
        }

        public void AddSound(string defName, Pawn pawn, float chance, Thing source)
        {
            var def = GetBaseSoundDefFor(defName, pawn);
            if (def != null)
            {
                var secondsCooldown = VoiceActedRadioChatterMod.GetCooldownSecondsFor(defName);
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
}
