using System;
using System.Collections.Generic;
using System.Text;
using HairCarePlus.Shared.Domain.Restrictions;

namespace HairCarePlus.Client.Patient.Features.Progress.Services.Implementation;

/// <summary>
/// Centralised helper for converting between <see cref="RestrictionIconType"/> and
/// UI-level artefacts (PNG filename / FontAwesome glyph).
/// Eliminates duplication in converters & adapters.
/// </summary>
public static class RestrictionIconMapper
{
    private static readonly Dictionary<RestrictionIconType, string> _pngFiles = new()
    {
        { RestrictionIconType.NoSmoking,      "no_smoking.png" },
        { RestrictionIconType.NoAlcohol,      "no_alchohol.png" }, // legacy typo kept for asset name
        { RestrictionIconType.NoSex,          "no_sex.png" },
        { RestrictionIconType.NoHairCutting,  "no_haircuting.png" }, // legacy typo kept
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

    /// <summary>
    /// Get PNG asset filename for given restriction icon.
    /// </summary>
    public static string ToPng(RestrictionIconType type) => _pngFiles.TryGetValue(type, out var name) ? name : "no_smoking.png";

    /// <summary>
    /// Get FontAwesome glyph (fallback) for given icon.
    /// </summary>
    public static string ToFaGlyph(RestrictionIconType type) => _fontAwesome.TryGetValue(type, out var glyph) ? glyph : "\uf54d";

    /// <summary>
    /// Rough heuristic mapping from free-form title to <see cref="RestrictionIconType"/>.
    /// Consolidates logic previously duplicated in multiple classes.
    /// </summary>
    public static RestrictionIconType FromTitle(string title)
    {
        var t = title.ToLowerInvariant();

        return t switch
        {
            var s when s.Contains("курен")  || s.Contains("сигарет") || s.Contains("табак")  => RestrictionIconType.NoSmoking,
            var s when s.Contains("алкогол")                                  => RestrictionIconType.NoAlcohol,
            var s when s.Contains("спорт")  || s.Contains("физическ")         => RestrictionIconType.NoSporting,
            var s when s.Contains("солнц")  || s.Contains("загар")            => RestrictionIconType.NoSun,
            var s when s.Contains("стрижк") || s.Contains("брить")            => RestrictionIconType.NoHairCutting,
            var s when s.Contains("басс")   || s.Contains("плаван")           => RestrictionIconType.NoSwimming,
            var s when s.Contains("шляп")   || s.Contains("шапк")             => RestrictionIconType.NoHatWearing,
            var s when s.Contains("наклон") || s.Contains("голов")            => RestrictionIconType.NoTilting,
            var s when s.Contains("лежать") || s.Contains("спать")            => RestrictionIconType.NoLaying,
            var s when s.Contains("секс")   || s.Contains("интим")            => RestrictionIconType.NoSex,
            var s when s.Contains("укладк") || s.Contains("стайлинг")         => RestrictionIconType.NoStyling,
            var s when s.Contains("пот")    || s.Contains("сауна")            => RestrictionIconType.NoSweating,
            _ => RestrictionIconType.NoSmoking
        };
    }
} 