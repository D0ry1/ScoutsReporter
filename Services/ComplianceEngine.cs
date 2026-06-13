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

    /// <summary>Experimental: bulk data from the Scouts compliance dashboard views.</summary>
    ScoutsBackend,
}
