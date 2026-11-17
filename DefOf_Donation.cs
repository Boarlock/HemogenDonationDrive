using RimWorld;
using Verse;

namespace HemogenDonation;

[DefOf]
public static class DefOf_Donation
{
    [MayRequireBiotech]
    public static RecipeDef? ExtractHemogenPack_HemogenDonation;

    [MayRequireBiotech]
    public static ThoughtDef? DonatedHemogenVoluntarily;
    public static ThoughtDef? DonatedHemogenBloodlust;
    public static ThoughtDef? DonatedHemogenRevered;

    [MayRequireBiotech]
    public static PreceptDef? Bloodfeeders_Revered;

    static DefOf_Donation() {

        DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_Donation));
    }
}
