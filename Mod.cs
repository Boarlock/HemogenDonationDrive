using Verse;

namespace HemogenDonation;

public static class ModStartup {

    internal static bool modActive = false;
}

public class MyMod : Mod {

    public MyMod(ModContentPack content) : base(content) {

        if (!ModsConfig.BiotechActive)
            return;

        ModStartup.modActive = true;
        Log.Message("Hemogen Donation loaded successfully");
    }
}
