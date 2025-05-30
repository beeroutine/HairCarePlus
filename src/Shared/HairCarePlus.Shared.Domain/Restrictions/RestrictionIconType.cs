namespace HairCarePlus.Shared.Domain.Restrictions;

/// <summary>
/// Unified list of restriction categories with corresponding UI icon.
/// Lives in Shared.Domain so all features (Calendar, Progress, etc.) reference the same enum.
/// </summary>
public enum RestrictionIconType
{
    NoSmoking,
    NoAlcohol,
    NoSex,
    NoHairCutting,
    NoHatWearing,
    NoStyling,
    NoLaying,
    NoSun,
    NoSweating,
    NoSwimming,
    NoSporting,
    NoTilting
} 