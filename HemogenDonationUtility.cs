using RimWorld;
using Verse;

namespace HemogenDonation;

public static class HemogenDonationUtility
{
    public static bool CanBeQueuedForDonation(Pawn pawn)
    {

        if (ModStartup.modActive == true &&
            pawn.Spawned &&
            pawn.BillStack is not null &&
            !pawn.BillStack.Bills.Any(x => x.recipe == RecipeDefOf.ExtractHemogenPack || x.recipe == DefOf_Donation.ExtractHemogenPack_HemogenDonation) &&
            PawnConsciousEnoughForDonation(pawn) &&
            DefOf_Donation.ExtractHemogenPack_HemogenDonation!.Worker.AvailableOnNow(pawn)) {

            return !pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss);
        }

        return false;
    }

    public static bool PawnConsciousEnoughForDonation(Pawn pawn) {

        return pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) > 0.45f;
    }
}
