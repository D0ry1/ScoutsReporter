namespace ScoutsReporter.Services;

/// <summary>
/// Which data-acquisition strategy the DBS + training reports use. Experimental toggle so
/// testers can compare results and speed (see docs/superpowers/specs/2026-06-13-…).
/// </summary>
public enum ComplianceEngine
{
    /// <summary>Sequential per-contact fetches + POR RoleRequirements. The validated baseline.</summary>
    Standard,

    /// <summary>Identical results to Standard, but per-contact fetches run concurrently (faster).</summary>
    Parallel,

    /// <summary>
    /// Not implemented — dropped after a spike (the Scouts compliance views only hold
    /// non-compliant/in-progress rows, so they can't enumerate all members for the reports).
    /// Retained for documentation; falls back to Standard. See the design spec's Outcome section.
    /// </summary>
    ScoutsBackend,
}
