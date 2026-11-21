using UnityEngine;
using RimWorld;
using Verse;

namespace HemogenDonation;

public class DonorChair_MinifiedThing : MinifiedThing {

    private const float CrateWidth = 1.276f;  // vanilla width for a 2x1
    private const float CrateHeight = 0.85f;   // vanilla is 0.638; tune this
    private const float CrateTop = 0.319f;  // vanilla top edge, kept fixed

    private static Vector3 CrateOffset 
        => new(0f, 0f, CrateTop - CrateHeight / 2f);

    private ThingOwner? innerContainer;
    private Graphic? crateFrontGraphic;

    private Graphic CrateFrontGraphic {

        get {

            crateFrontGraphic ??= LoadCrateFrontGraphic();

            return crateFrontGraphic;
        }
    }

    public override Graphic Graphic {

        get {

            if (cachedGraphic == null) {

                cachedGraphic = InnerThing.Graphic.ExtractInnerGraphicFor(InnerThing);

                if ((float)InnerThing.def.size.x > 1.1f || (float)InnerThing.def.size.z > 1.1f) {

                    Vector2 minifiedDrawSize = GetMinifiedDrawSize(InnerThing.def.size.ToVector2(), 1.1f);
                    Vector2 newDrawSize = new Vector2(minifiedDrawSize.x / (float)InnerThing.def.size.x * cachedGraphic.drawSize.x, minifiedDrawSize.y / (float)InnerThing.def.size.z * cachedGraphic.drawSize.y);
                    cachedGraphic = cachedGraphic.GetCopy(newDrawSize, null);
                }

                if (Math.Abs(InnerThing.def.minifiedDrawScale - 1f) > float.Epsilon)
                    cachedGraphic = cachedGraphic.GetCopy(new Vector2(InnerThing.def.minifiedDrawScale * cachedGraphic.drawSize.x, InnerThing.def.minifiedDrawScale * cachedGraphic.drawSize.y), null);
            }

            return cachedGraphic;
        }
    }

    protected override Graphic LoadCrateFrontGraphic()
        => GraphicDatabase.Get<Graphic_Single>("Things/Item/Minified/CrateFront", ShaderDatabase.Cutout, new Vector2(CrateWidth, CrateHeight), Color.white);

    protected override void DrawAt(
        Vector3 drawLoc, 
        bool flip = false) {

        if (InnerThing.def.minifiedManualDraw) {

            InnerThing.DrawNowAt(drawLoc, flip);
            CrateFrontGraphic.DrawFromDef(drawLoc + CrateOffset + Altitudes.AltIncVect * 0.1f, Rot4.North, null);
            return;
        }

        CrateFrontGraphic.DrawFromDef(drawLoc + CrateOffset + Altitudes.AltIncVect * 0.1f, Rot4.North, null);
        Rot4 rot = ((Graphic is Graphic_Single) ? Rot4.North : Rot4.South);

        if (InnerThing.def.overrideMinifiedRot != Rot4.Invalid)
            rot = InnerThing.def.overrideMinifiedRot;

        Vector3 vector = InnerThing.def.minifiedDrawOffset - Graphic.DrawOffset(rot);
        Graphic.Draw(drawLoc + vector, rot, this);
    }

    protected new Vector2 GetMinifiedDrawSize(
        Vector2 drawSize, 
        float maxSideLength) {

        float num = maxSideLength / Mathf.Max(drawSize.x, drawSize.y);

        if (num >= 1f)
            return drawSize;

        return drawSize * num;
    }
}
