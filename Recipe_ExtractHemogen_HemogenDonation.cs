using RimWorld;
using Verse;

namespace HemogenDonation;

public class Recipe_ExtractHemogen_HemogenDonation : Recipe_ExtractHemogen
{
    protected override void OnSurgerySuccess(
    Pawn donor,
    BodyPartRecord part,
    Pawn doctor,
    List<Thing> ingredients,
    Bill bill) {

        base.OnSurgerySuccess(donor, part, doctor, ingredients, bill);

        ThoughtDef thought = DefOf_Donation.DonatedHemogenVoluntarily!;

        if (donor.story?.traits?.HasTrait(TraitDefOf.Bloodlust) == true)
            thought = DefOf_Donation.DonatedHemogenBloodlust!;

        else if (ModsConfig.IdeologyActive &&
            donor.Ideo is not null &&
            donor.Ideo.HasPrecept(DefOf_Donation.Bloodfeeders_Revered))
            thought = DefOf_Donation.DonatedHemogenRevered!;

        donor.needs?.mood?.thoughts?.memories?.TryGainMemory(thought, doctor);
    }
}

