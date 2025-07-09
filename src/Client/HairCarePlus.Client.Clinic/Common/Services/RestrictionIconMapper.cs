using System.Collections.Generic;
using HairCarePlus.Shared.Domain.Restrictions;

namespace HairCarePlus.Client.Clinic.Common.Services;

/// <summary>
/// Centralised helper for converting between <see cref="RestrictionIconType"/> and
/// UI artefacts (PNG filename / FontAwesome glyph).
/// </summary>
public static class RestrictionIconMapper
{
    private static readonly Dictionary<RestrictionIconType, string> _pngFiles = new()
    {
        { RestrictionIconType.NoSmoking,      "no_smoking.png" },
        { RestrictionIconType.NoAlcohol,      "no_alchohol.png" },
        { RestrictionIconType.NoSex,          "no_sex.png" },
        { RestrictionIconType.NoHairCutting,  "no_haircuting.png" },
        { RestrictionIconType.NoHatWearing,   "no_hat_wearing.png" },
        { RestrictionIconType.NoStyling,      "no_styling.png" },
        { RestrictionIconType.NoLaying,       "no_laying.png" },
        { RestrictionIconType.NoSun,          "no_sun.png" },
        { RestrictionIconType.NoSweating,     "no_sweating.png" },
        { RestrictionIconType.NoSwimming,     "no_swimming.png" },
        { RestrictionIconType.NoSporting,     "no_sporting.png" },
        { RestrictionIconType.NoTilting,      "no_tilting.png" }
    };

    private static readonly Dictionary<RestrictionIconType, string> _fontAwesome = new()
    {
        { RestrictionIconType.NoSmoking,  "\uf54d" },
        { RestrictionIconType.NoAlcohol,  "\uf5c8" },
        { RestrictionIconType.NoSex,      "\uf3c4" },
        { RestrictionIconType.NoHairCutting, "\uf2c7" },
        { RestrictionIconType.NoHatWearing,  "\uf5c1" },
        { RestrictionIconType.NoStyling,     "\uf5c3" },
        { RestrictionIconType.NoLaying,      "\uf564" },
        { RestrictionIconType.NoSun,         "\uf05e" },
        { RestrictionIconType.NoSweating,    "\uf769" },
        { RestrictionIconType.NoSwimming,    "\uf5c4" },
        { RestrictionIconType.NoSporting,    "\uf70c" },
        { RestrictionIconType.NoTilting,     "\uf3c4" }
    };

    public static string ToPng(RestrictionIconType type) => _pngFiles.TryGetValue(type, out var name) ? name : "no_smoking.png";

    public static string ToFaGlyph(RestrictionIconType type) => _fontAwesome.TryGetValue(type, out var glyph) ? glyph : "\uf54d";
} 