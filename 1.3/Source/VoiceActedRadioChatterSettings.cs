using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VoiceActedRadioChatter
{
    public class CooldownValue : IExposable
    {
        public int min;
        public int max;
        public int GetValue => new IntRange(min, max).RandomInRange;
        public void ExposeData()
        {
            Scribe_Values.Look(ref min, "min");
            Scribe_Values.Look(ref max, "max");
        }
    }
    public class VoiceActedRadioChatterSettings : ModSettings
    {
        private int scrollHeightCount = 0;
        private Vector2 scrollPosition;
        public static List<string> disabledVoiceActors = new List<string>();
        public static Dictionary<string, CooldownValue> cooldownSecondsBySounds;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref disabledVoiceActors, "disabledVoiceActors");
            Scribe_Collections.Look(ref cooldownSecondsBySounds, "cooldownSecondsBySounds");
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            var outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 30, scrollHeightCount);
            scrollHeightCount = 0;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(viewRect);
            listingStandard.Label("VARC.CooldownForUnitResponses".Translate());
            listingStandard.Label("VARC.EnableDisableVoiceActors".Translate());
            scrollHeightCount += 24;
            foreach (var actor in VoiceActedRadioChatterMod.voiceActors)
            {
                scrollHeightCount += 24;
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
            Widgets.EndScrollView();
        }
    }
}
