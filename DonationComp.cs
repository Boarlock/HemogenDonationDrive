using Verse;

namespace HemogenDonation;

public enum DonationStage { Idle, Called, Seated, HookedUp }

public class CompProperties_Donation : CompProperties {

    public int ticksToExtract;

    public CompProperties_Donation() {

        // REQUIRED: Tell RimWorld which comp class to instantiate
        compClass = typeof(CompDonation);
    }
}

public class CompDonation : ThingComp {

    // Typed access to our CompProperties
    public CompProperties_Donation Props => (CompProperties_Donation)props;

    private DonationStage stage;
    private Pawn? doctor, donor;

    public Pawn? Doctor => doctor;
    public Pawn? Donor => donor;
    public DonationStage Stage => stage;
    public bool IsActive => stage != DonationStage.Idle;

    public void Begin(Pawn doc, Pawn don) { 
        
        doctor = doc; 
        donor = don; 
        stage = DonationStage.Called; 
    }

    public void Advance(DonationStage s) => stage = s;
    public void Reset() { 

        stage = DonationStage.Idle; 
        doctor = donor = null; 
    
    }

    // Called after the thing is spawned on the map
    public override void PostSpawnSetup(bool respawningAfterLoad) {

        base.PostSpawnSetup(respawningAfterLoad);

    }

    public override void CompTick() {

        base.CompTick();

    }

    // Serialization for save/load
    public override void PostExposeData() {

        base.PostExposeData();

        Scribe_Values.Look(ref stage, "stage", DonationStage.Idle);
        Scribe_References.Look(ref doctor, "doctor");
        Scribe_References.Look(ref donor, "donor");
    }

    // Add UI buttons when selected
    public override IEnumerable<Gizmo> CompGetGizmosExtra() {

        if (IsActive) {

            yield return new Command_Action {

                defaultLabel = "Cancel Operation",
                action = () => Reset()
            };
        }
    }
}
