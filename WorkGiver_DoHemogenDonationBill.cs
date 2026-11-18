using RimWorld;
using Verse;
using Verse.AI;

namespace HemogenDonation;

public class WorkGiver_DoHemogenDonationBill : WorkGiver_Scanner {

    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefOf_Donation.DonationChair);

    public override bool ShouldSkip(
        Pawn pawn, 
        bool forced = false) {

        List<Thing> chairs = pawn.Map.listerThings.ThingsOfDef(DefOf_Donation.DonationChair);

        for (int i = 0; i < chairs.Count; i++) {

            if (chairs[i] is IBillGiver billGiver && 
                billGiver.BillStack.AnyShouldDoNow) {

                return false;
            }
        }
        return true;
    }

    public override Job? JobOnThing(
        Pawn pawn, 
        Thing thing, 
        bool forced = false) {

        if ((thing is not IBillGiver billGiver) || 
            !pawn.CanReserve(thing, 1, -1, null, forced) || 
            thing.IsBurning()) {

            return null;
        }

        if (thing.def.hasInteractionCell && 
            !pawn.CanReserveSittableOrSpot(thing.InteractionCell, 
            thing, 
            forced)) {

            return null;
        }

        billGiver.BillStack.RemoveIncompletableBills();
        return StartOrResumeBillJob(pawn, billGiver, forced);
    }

    private static readonly IntRange ReCheckFailedBillTicksRange = new(500, 600);

    private Job? StartOrResumeBillJob(
        Pawn doctor, 
        IBillGiver giver, 
        bool forced = false) {

        Thing chair = (Thing)giver;
        bool makingFloatMenu = FloatMenuMakerMap.makingFor == doctor;

        for (int i = 0; i < giver.BillStack.Count; i++) {

            Bill bill = giver.BillStack[i];

            if ((bill.recipe.requiredGiverWorkType != null &&
                bill.recipe.requiredGiverWorkType != def.workType) ||
                (!makingFloatMenu && Find.TickManager.TicksGame <= bill.nextTickToSearchForIngredients) ||
                !bill.ShouldDoNow() ||
                !bill.PawnAllowedToStartAnew(doctor)) {

                continue;
            }

            SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(doctor);

            if (skillRequirement != null) {

                JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
                continue;
            }

            Pawn? donor = FindDonor(doctor, chair, forced);

            if (donor == null) {

                if (makingFloatMenu)
                    JobFailReason.Is("no available donor", bill.Label);
                else
                    bill.nextTickToSearchForIngredients = Find.TickManager.TicksGame + ReCheckFailedBillTicksRange.RandomInRange;

                continue;
            }

            Job job = JobMaker.MakeJob(DefOf_Donation.DoHemogenDonationBill, chair, donor);
            job.bill = bill;
            return job;
        }

        return null;
    }

    private Pawn? FindDonor(
        Pawn doctor, 
        Thing chair, 
        bool forced)
    {

        Pawn? best = null;
        int bestDist = int.MaxValue;

        foreach (Pawn candidate in doctor.Map.mapPawns.FreeColonistsSpawned) {

            if (candidate == doctor ||
                !HemogenDonationUtility.IsEligibleDonor(candidate) ||
                !doctor.CanReserve(candidate, 1, -1, null, forced) ||
                !candidate.CanReach(chair.InteractionCell, PathEndMode.OnCell, Danger.Deadly)) {

                continue;
            }

            int dist = (candidate.Position - chair.Position).LengthHorizontalSquared;

            if (dist < bestDist) {

                best = candidate;
                bestDist = dist;
            }
        }

        return best;
    }
}
