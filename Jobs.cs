using RimWorld;
using Verse;
using Verse.AI;

namespace HemogenDonation;

public class JobDriver_HookupDonor : JobDriver {

    CompDonation Comp => job.targetA.Thing.TryGetComp<CompDonation>();
    Pawn Donor => (Pawn)job.targetB.Thing;

    // Reserve required targets before job begins
    public override bool TryMakePreToilReservations(bool errorOnFailed)
        => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

    // Define the sequence of toils
    protected override IEnumerable<Toil> MakeNewToils() {

        // Fail conditions applied to all toils
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOnDowned(TargetIndex.B);

        // Anything but success cancels the donation, which fails the donor.
        AddFinishAction(cond => {

            if (cond != JobCondition.Succeeded && 
            Comp is { } chair && 
            chair.Doctor == pawn)
                chair.Reset();
        });

        yield return new Toil {

            defaultCompleteMode = ToilCompleteMode.Instant,
            initAction = () => {
                
                if (Comp.IsActive) { 

                    EndJobWith(JobCondition.Incompletable); 
                    return; 
                }

                Comp.Begin(pawn, Donor);

                Job donorJob = JobMaker.MakeJob(DefOf_Donation.DonateHemogen, job.targetA, pawn);
                donorJob.bill = job.bill;

                if (!Donor.jobs.TryTakeOrderedJob(donorJob)) {

                    Comp.Reset();
                    EndJobWith(JobCondition.Incompletable);
                }
            }
        };

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOn(() => !Comp.IsActive);

        Toil waitSeated = new Toil { 

            defaultCompleteMode = ToilCompleteMode.Never,
            tickAction = () => { 
                
                if (Comp.Stage == DonationStage.Seated) 
                    ReadyForNextToil(); 
            }
        };

        waitSeated.FailOn(() => !Comp.IsActive);
        yield return waitSeated;

        Toil hookup = ToilMaker.MakeToil("Hookup");
        hookup.defaultCompleteMode = ToilCompleteMode.Delay;
        hookup.defaultDuration = 300;
        hookup.WithProgressBarToilDelay(TargetIndex.A);
        hookup.tickAction = () => pawn.skills?.Learn(SkillDefOf.Medicine, 0.1f);
        hookup.FailOn(() => !Comp.IsActive);
        yield return hookup;

        yield return new Toil {

            defaultCompleteMode = ToilCompleteMode.Instant,
            initAction = () => Comp.Advance(DonationStage.HookedUp)
        };
    }
}

public class JobDriver_DonateHemogen : JobDriver {

    CompDonation Comp => job.targetA.Thing.TryGetComp<CompDonation>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils() {

        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDowned(TargetIndex.B);                    // B is the doctor
        this.FailOn(() => Comp is not { IsActive: true });   // doctor failed, or donation was reset

        AddFinishAction(cond => {

            if (Comp is not { } chair || chair.Donor != pawn) 
                return;

            if (cond == JobCondition.Succeeded) {

                HemogenDonationUtility.OnSurgerySuccess(pawn);
                job.bill.Notify_IterationCompleted(chair.Doctor, []);
            }
            
            chair.Reset();
        });
        IntVec3 interactionCell = job.targetA.Thing.Position + job.targetA.Thing.Rotation.RighthandCell;
        yield return Toils_Goto.GotoThing(TargetIndex.A, interactionCell);

        Toil waitHookup = new Toil { 
            
            defaultCompleteMode = ToilCompleteMode.Never,
            initAction = () => Comp.Advance(DonationStage.Seated),
            tickAction = () => { 
                
                if (Comp.Stage == DonationStage.HookedUp) 
                    ReadyForNextToil(); 
            }
        };

        yield return waitHookup;

        Toil donate = ToilMaker.MakeToil("Donate");
        donate.defaultCompleteMode = ToilCompleteMode.Delay;
        donate.defaultDuration = Comp.Props.ticksToExtract;
        donate.WithProgressBarToilDelay(TargetIndex.A);
        yield return donate;
    }
}
