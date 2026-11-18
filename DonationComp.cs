using UnityEngine;
using Verse;

namespace HemogenDonation;

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

    private bool isActive;
    private int ticksToComplete;

    private const int progressBarCells = 20;
    private int progressBarFilled = -1;
    private string? progressBar;

    private void CompleteDonation() {

        progressBar = null;
        progressBarFilled = -1;
        isActive = false;
    }

    private void CancelDonation() {

        CompleteDonation();
    }

    // Called after the thing is spawned on the map
    public override void PostSpawnSetup(bool respawningAfterLoad) {

        base.PostSpawnSetup(respawningAfterLoad);


    }

    public override void CompTick() {

        base.CompTick();

        if (!isActive) 
            return;

        if (ticksToComplete <= 0) {

            CompleteDonation(); // apply blood loss, drop pack, then FinishDonation()
            return;
        }

        ticksToComplete--;

        int total = Props.ticksToExtract;
        int elapsed = total - ticksToComplete;
        float fraction = total > 0 ? Mathf.Clamp01((float)elapsed / total) : 0f;
        int filled = Mathf.FloorToInt(fraction * progressBarCells);

        if (filled != progressBarFilled) {

            progressBarFilled = filled;
            progressBar = "Donation Progress\n["
                + new string('█', filled)
                + new string('░', progressBarCells - filled)
                + "] " + Mathf.FloorToInt(fraction * 100f) + "%";
        }
    }

    // Serialization for save/load
    public override void PostExposeData() {

        base.PostExposeData();

        Scribe_Values.Look(ref isActive, "active", false);
        Scribe_Values.Look(ref ticksToComplete, "toComplete", 0);
    }

    public override string? CompInspectStringExtra() => progressBar;

    // Add UI buttons when selected
    public override IEnumerable<Gizmo> CompGetGizmosExtra() {

        if (isActive) {

            yield return new Command_Action
            {

                defaultLabel = "Cancel Operation",
                action = () => CancelDonation()
            };
        }
    }
}
