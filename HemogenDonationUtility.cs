using RimWorld;
using Verse;

namespace HemogenDonation;

public static class HemogenDonationUtility {

    internal static bool IsEligibleDonor(Pawn pawn) 
        => ModStartup.modActive == true &&
            pawn.Spawned && !pawn.Downed && !pawn.Drafted &&
            !pawn.DevelopmentalStage.Baby() &&
            pawn.health.CanBleed &&
            !(pawn.genes?.HasActiveGene(GeneDefOf.Hemogenic) ?? false) &&
            !pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss) &&
            pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) > 0.45f;

    internal static void OnSurgerySuccess(Pawn pawn) {

        Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
        hediff.Severity = 0.45f;
        pawn.health.AddHediff(hediff);

        if (!GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.HemogenPack), pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near)) {

            Log.Error("Could not drop hemogen pack near " + pawn.PositionHeld.ToString());
        }
    }
}
