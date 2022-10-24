using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
        public static List<string> disabledUnitResponses = new List<string>();
        public static List<string> disabledEventSounds = new List<string>();
        public static Dictionary<string, string> fixedVoicesForPawns = new Dictionary<string, string>();
        public static Dictionary<string, CooldownValue> cooldownSecondsBySounds;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref disabledVoiceActors, "disabledVoiceActors");
            Scribe_Collections.Look(ref disabledUnitResponses, "disabledUnitResponses");
            Scribe_Collections.Look(ref cooldownSecondsBySounds, "cooldownSecondsBySounds");
            Scribe_Collections.Look(ref disabledEventSounds, "disabledEventSounds");
            Scribe_Collections.Look(ref fixedVoicesForPawns, "fixedVoicesForPawns", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (disabledVoiceActors is null)
                {
                    disabledVoiceActors = new List<string>();
                }
                if (disabledUnitResponses is null)
                {
                    disabledUnitResponses = new List<string>();
                }
                if (cooldownSecondsBySounds is null)
                {
                    cooldownSecondsBySounds = new Dictionary<string, CooldownValue>();
                }
                if (fixedVoicesForPawns is null)
                {
                    fixedVoicesForPawns = new Dictionary<string, string>();
                }
                if (disabledEventSounds is null)
                {
                    disabledEventSounds = new List<string>();
                }
            }
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            var outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 30, scrollHeightCount);
            scrollHeightCount = 0;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(viewRect);

            listingStandard.Label("VARC.EnableDisableEventSounds".Translate());
            scrollHeightCount += 24;
            foreach (var eventSound in VoiceActedRadioChatterStartup.eventSounds)
            {
                var name = GenText.SplitCamelCase(eventSound.defName.Replace(VoiceActedRadioChatterMod.Prefix, ""));
                listingStandard.Label(name);
                var rect = new Rect(listingStandard.curX + 200, listingStandard.curY - 24, 600, 24);

                var enabled = disabledEventSounds.Contains(eventSound.defName) is false;
                Widgets.Checkbox(rect.xMax + 10, rect.y, ref enabled);
                if (enabled)
                {
                    if (disabledEventSounds.Contains(eventSound.defName))
                    {
                        disabledEventSounds.Remove(eventSound.defName);
                    }
                }
                else
                {
                    if (!disabledEventSounds.Contains(eventSound.defName))
                    {
                        disabledEventSounds.Add(eventSound.defName);
                    }
                }
                scrollHeightCount += 24;
            }

            listingStandard.GapLine();
            scrollHeightCount += 12;

            if (Current.Game != null)
            {
                listingStandard.Label("VARC.RandomizeOrSetVoicesForColonists".Translate());
                scrollHeightCount += 24;
                foreach (var colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
                {
                    var label = new Rect(listingStandard.curX, listingStandard.curY, 400, 24);
                    Widgets.Label(label, colonist.Name.ToStringFull);

                    var voiceButton = new Rect(label.xMax, label.y, 250, 24);
                    var curVoice = VoiceActedRadioChatterMod.GetVoiceFor(colonist);
                    if (Widgets.ButtonText(voiceButton, curVoice))
                    {
                        var floatMenuList = new List<FloatMenuOption>();
                        var gender = colonist.gender == Gender.Male ? "Male" : "Female";
                        var voicePool = VoiceActedRadioChatterMod.GetVoicePool(colonist);
                        foreach (var voice in voicePool)
                        {
                            floatMenuList.Add(new FloatMenuOption(voice, delegate
                            {
                                fixedVoicesForPawns[colonist.Name.ToStringFull] = voice;
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(floatMenuList));
                    }
                    var randomize = new Rect(voiceButton.xMax + 15, voiceButton.y, 150, 24);
                    if (Widgets.ButtonText(randomize, "Randomize".Translate()))
                    {
                        var gender = colonist.gender == Gender.Male ? "Male" : "Female";
                        var voicePool = VoiceActedRadioChatterMod.GetVoicePool(colonist).Where(x => x != curVoice).ToList();
                        fixedVoicesForPawns[colonist.Name.ToStringFull] = voicePool.RandomElement();
                    }

                    listingStandard.curY += 24;
                    scrollHeightCount += 24;
                }
                listingStandard.GapLine();
                scrollHeightCount += 12;
            }

            listingStandard.Label("VARC.CooldownForUnitResponses".Translate());
            scrollHeightCount += 24;
            foreach (var unitResponse in VoiceActedRadioChatterMod.unitResponses)
            {
                var cooldownValue = cooldownSecondsBySounds[unitResponse];
                var range = new IntRange(cooldownValue.min, cooldownValue.max);
                listingStandard.Label(GenText.SplitCamelCase(unitResponse));
                var rect = new Rect(listingStandard.curX + 200, listingStandard.curY - 24, 600, 24);
                Widgets.IntRange(rect, (int)listingStandard.CurHeight, ref range, 0, 60);
                cooldownValue.min = range.min; 
                cooldownValue.max = range.max;

                var enabled = disabledUnitResponses.Contains(unitResponse) is false;
                Widgets.Checkbox(rect.xMax + 10, rect.y, ref enabled);
                if (enabled)
                {
                    if (disabledUnitResponses.Contains(unitResponse))
                    {
                        disabledUnitResponses.Remove(unitResponse);
                    }
                }
                else
                {
                    if (!disabledUnitResponses.Contains(unitResponse))
                    {
                        disabledUnitResponses.Add(unitResponse);
                    }
                }
                scrollHeightCount += 24;
            }

            listingStandard.GapLine();
            scrollHeightCount += 12;
            listingStandard.Label("VARC.EnableDisableVoiceActors".Translate());
            scrollHeightCount += 24;
            foreach (var actor in VoiceActedRadioChatterMod.voiceActors)
            {
                scrollHeightCount += 24;
                var enabled = disabledVoiceActors.Contains(actor) is false;
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
