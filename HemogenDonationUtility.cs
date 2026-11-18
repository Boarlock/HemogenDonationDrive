using RimWorld;
using Verse;

namespace HemogenDonation;

public static class HemogenDonationUtility {

    internal static bool IsEligibleDonor(Pawn pawn) {

        if (ModStartup.modActive == true &&
            pawn.Spawned && !pawn.Downed && !pawn.Drafted &&
            !pawn.DevelopmentalStage.Baby() &&
            pawn.health.CanBleed &&
            !(pawn.genes?.HasActiveGene(GeneDefOf.Hemogenic) ?? false) &&
            !pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss) &&
            pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) > 0.45f) {

            return true;
        }

        return false;
    }
}
