using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace HemogenDonation;

public class DonorChair_Graphic : Graphic {

    private struct AtlasReplacementInfoCacheKey : IEquatable<AtlasReplacementInfoCacheKey> {
        public readonly Material mat;
        public readonly TextureAtlasGroup group;
        public readonly bool flipUv;
        public readonly bool vertexColors;
        private readonly int hash;

        public AtlasReplacementInfoCacheKey(Material mat, TextureAtlasGroup group, bool flipUv, bool vertexColors) {

            this.mat = mat;
            this.group = group;
            this.flipUv = flipUv;
            this.vertexColors = vertexColors;

            hash = Gen.HashCombine(mat.GetHashCode(), group.GetHashCode());

            if (flipUv)
                hash = ~hash;

            if (vertexColors)
                hash ^= 123893723;
        }

        public bool Equals(AtlasReplacementInfoCacheKey other) {

            if ((object)mat == other.mat && group == other.group && flipUv == other.flipUv)
                return vertexColors == other.vertexColors;

            return false;
        }

        public override int GetHashCode() => hash;
    }

    private struct CachedAtlasReplacementInfo {
        public Material material;
        public Vector2[] uvs;
        public Color32 vertexColor;
    }

    private Graphic_Shadow? cachedShadowGraphicInt;
    private Graphic? cachedShadowlessGraphicInt;
    private static Dictionary<AtlasReplacementInfoCacheKey, CachedAtlasReplacementInfo> replacementInfoCache = [];

    public new Graphic_Shadow ShadowGraphic {

        get {

            if (cachedShadowGraphicInt == null && data != null && data.shadowData != null) {

                cachedShadowGraphicInt = new DonorChair_Graphic_Shadow(data.shadowData);
            }

            return cachedShadowGraphicInt!;
        }
        set => cachedShadowGraphicInt = value;
    }

    public override void DrawWorker(
        Vector3 loc, 
        Rot4 rot, 
        ThingDef thingDef, 
        Thing thing, 
        float extraRotation) {

        Mesh mesh = MeshAt(rot);
        Quaternion quat = QuatFromRot(rot);

        if (extraRotation != 0f)
            quat *= Quaternion.Euler(Vector3.up * extraRotation);

        if (data != null && data.addTopAltitudeBias)
            quat *= Quaternion.Euler(Vector3.left * 2f);

        loc += DrawOffset(rot);
        Material mat = MatAt(rot, thing);
        DrawMeshInt(mesh, loc, quat, mat);

        if (thing is not MinifiedThing)
            ShadowGraphic?.DrawWorker(loc, rot, thingDef, thing, extraRotation);
    }

    public override void Print(SectionLayer layer, Thing thing, float extraRotation) {

        Vector2 size;
        bool flag;

        if (ShouldDrawRotated) {

            size = drawSize;
            flag = false;
        }
        else {

            size = (thing.Rotation.IsHorizontal ? drawSize.Rotated() : drawSize);
            flag = (thing.Rotation == Rot4.West && WestFlipped) || (thing.Rotation == Rot4.East && EastFlipped);
        }

        if (thing.MultipleItemsPerCellDrawn())
            size *= 0.8f;

        float num = AngleFromRot(thing.Rotation) + extraRotation;

        if (flag && data != null)
            num += data.flipExtraRotation;

        Vector3 center = thing.TrueCenter() + DrawOffset(thing.Rotation);
        Material material = MatAt(thing.Rotation, thing);
        TryGetTextureAtlasReplacementInfo(material, thing.def.category.ToAtlasGroup(), flag, vertexColors: true, out material, out var uvs, out var vertexColor);
        Printer_Plane.PrintPlane(layer, center, size, material, num, flag, uvs, new Color32[4] { vertexColor, vertexColor, vertexColor, vertexColor });

        if (thing is not MinifiedThing)
            ShadowGraphic?.Print(layer, thing, 0f);
    }
}

public class DonorChair_Graphic_Shadow : Graphic_Shadow {

    private Mesh shadowMesh;
    private ShadowData shadowInfo;

    [TweakValue("Graphics_Shadow", -5f, 5f)]
    private static float GlobalShadowPosOffsetX;

    [TweakValue("Graphics_Shadow", -5f, 5f)]
    private static float GlobalShadowPosOffsetZ;

    public DonorChair_Graphic_Shadow(ShadowData shadowInfo) : base(shadowInfo) {

        this.shadowInfo = shadowInfo;
        shadowMesh = ShadowMeshPool.GetShadowMesh(shadowInfo);
    }

    private Vector3 OffsetFor(Rot4 rot) {

        if (rot == Rot4.West) {

            // mirror of east: keep east's z, flip x
            Vector3 e = shadowInfo.offset.RotatedBy(Rot4.East);
            return new Vector3(-e.x, e.y, e.z);
        }

        return shadowInfo.offset.RotatedBy(rot);
    }

    public override void DrawWorker(
        Vector3 loc, 
        Rot4 rot, 
        ThingDef thingDef, 
        Thing thing, 
        float extraRotation) {

        if (shadowMesh != null && 
            thingDef != null && 
            shadowInfo != null && 
            (Find.CurrentMap == null || !Find.CurrentMap.Biome.disableShadows) && 
            (Find.CurrentMap == null || !loc.ToIntVec3().InBounds(Find.CurrentMap) || 
            !Find.CurrentMap.roofGrid.Roofed(loc.ToIntVec3())) && DebugViewSettings.drawShadows) {

            Vector3 position = loc + OffsetFor(rot);
            position.y = AltitudeLayer.Shadows.AltitudeFor();
            Graphics.DrawMesh(shadowMesh, position, rot.AsQuat, MatBases.SunShadowFade, 0);
        }
    }

    public override void Print(SectionLayer layer, Thing thing, float extraRotation) {

        Vector3 center = thing.TrueCenter()
            + OffsetFor(thing.Rotation)
            + new Vector3(GlobalShadowPosOffsetX, 0f, GlobalShadowPosOffsetZ).RotatedBy(thing.Rotation);

        center.y = AltitudeLayer.Shadows.AltitudeFor();
        Printer_Shadow.PrintShadow(layer, center, shadowInfo, thing.Rotation);
    }
}

public class DonorChair_Graphic_Multi : DonorChair_Graphic {

    private Material[] mats = new Material[4];
    public override Material MatSingle => MatSouth;
    public override Material MatNorth => mats[0];
    public override Material MatEast => mats[1];
    public override Material MatSouth => mats[2];
    public override Material MatWest => mats[3];

    public override bool WestFlipped => westFlipped;
    public override bool EastFlipped => eastFlipped;
    public override float DrawRotatedExtraAngleOffset => drawRotatedExtraAngleOffset;
    private bool westFlipped;
    private bool eastFlipped;
    private float drawRotatedExtraAngleOffset;

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        => GraphicDatabase.Get<DonorChair_Graphic_Multi>(path, newShader, drawSize, newColor, newColorTwo, data, maskPath);

    public override int GetHashCode()
        => Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombine(0, path), color), colorTwo);

    public override string ToString()
        => "Multi(initPath=" + path + ", color=" + color.ToString() + ", colorTwo=" + colorTwo.ToString() + ")";

    public override bool ShouldDrawRotated {

        get {

            if (data == null || data.drawRotated)
                return MatEast == MatNorth || MatWest == MatNorth;

            return false;
        }
    }

    public override Material MatAt(Rot4 rot, Thing? thing = null) {

        switch (rot.AsInt) {

            case 0: 
                return MatNorth;
            case 1: 
                return MatEast;
            case 2: 
                return MatSouth;
            case 3: 
                return MatWest;
            default: 
                return BaseContent.BadMat;
        }
    }

    public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey) {

        Material[] array = mats;
        foreach (Material material in array) {

            Texture2D? mask = null;

            if (material.HasProperty(ShaderPropertyIDs.MaskTex))
                mask = (Texture2D)material.GetTexture(ShaderPropertyIDs.MaskTex);

            GlobalTextureAtlasManager.TryInsertStatic(groupKey, (Texture2D)material.mainTexture, mask);
        }
    }

    public override void Init(GraphicRequest req) {

        data = req.graphicData;
        path = req.path;
        maskPath = req.maskPath;
        color = req.color;
        colorTwo = req.colorTwo;
        drawSize = req.drawSize;
        Texture2D[] array = new Texture2D[mats.Length];
        array[0] = ContentFinder<Texture2D>.Get(req.path + "_north", reportFailure: false);
        array[1] = ContentFinder<Texture2D>.Get(req.path + "_east", reportFailure: false);
        array[2] = ContentFinder<Texture2D>.Get(req.path + "_south", reportFailure: false);
        array[3] = ContentFinder<Texture2D>.Get(req.path + "_west", reportFailure: false);

        if (array[0] == null) {

            if (array[2] != null) {

                array[0] = array[2];
                drawRotatedExtraAngleOffset = 180f;
            }
            else if (array[1] != null) {

                array[0] = array[1];
                drawRotatedExtraAngleOffset = -90f;
            }
            else if (array[3] != null) {

                array[0] = array[3];
                drawRotatedExtraAngleOffset = 90f;
            }
            else
                array[0] = ContentFinder<Texture2D>.Get(req.path, reportFailure: false);
        }

        if (array[0] == null) {

            Log.Error("Failed to find any textures at " + req.path + " while constructing " + this.ToStringSafe());
            mats[0] = (mats[1] = (mats[2] = (mats[3] = BaseContent.BadMat)));
            return;
        }

        if (array[2] == null)
            array[2] = array[0];

        if (array[1] == null) {

            if (array[3] != null) {

                array[1] = array[3];
                eastFlipped = base.DataAllowsFlip;
            }
            else
                array[1] = array[0];
        }

        if (array[3] == null) {

            if (array[1] != null) {

                array[3] = array[1];
                westFlipped = base.DataAllowsFlip;
            }
            else
                array[3] = array[0];
        }

        Texture2D[] array2 = new Texture2D[mats.Length];

        if (req.shader.SupportsMaskTex()) {

            string text = (maskPath.NullOrEmpty() ? path : maskPath);
            string text2 = (maskPath.NullOrEmpty() ? "m" : string.Empty);
            array2[0] = ContentFinder<Texture2D>.Get(text + "_north" + text2, reportFailure: false);
            array2[1] = ContentFinder<Texture2D>.Get(text + "_east" + text2, reportFailure: false);
            array2[2] = ContentFinder<Texture2D>.Get(text + "_south" + text2, reportFailure: false);
            array2[3] = ContentFinder<Texture2D>.Get(text + "_west" + text2, reportFailure: false);
            if (array2[0] == null) {

                if (array2[2] != null)
                    array2[0] = array2[2];
                else if (array2[1] != null)
                    array2[0] = array2[1];
                else if (array2[3] != null)
                    array2[0] = array2[3];
            }

            if (array2[2] == null)
                array2[2] = array2[0];

            if (array2[1] == null) {

                if (array2[3] != null)
                    array2[1] = array2[3];
                else
                    array2[1] = array2[0];
            }
            if (array2[3] == null) {

                if (array2[1] != null)
                    array2[3] = array2[1];
                else
                    array2[3] = array2[0];
            }
        }

        for (int i = 0; i < mats.Length; i++) {

            MaterialRequest req2 = new MaterialRequest {

                mainTex = array[i],
                shader = req.shader,
                color = color,
                colorTwo = colorTwo,
                maskTex = array2[i],
                shaderParameters = req.shaderParameters,
                renderQueue = req.renderQueue
            };

            mats[i] = MaterialPool.MatFrom(req2);
        }
    }
}
