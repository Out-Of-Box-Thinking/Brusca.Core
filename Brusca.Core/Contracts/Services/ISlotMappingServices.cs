using Brusca.Core.Models.Pii;
using FluentResults;

namespace Brusca.Core.Contracts.Services;

/// <summary>
/// Asks Claude to map the per-file <see cref="PiiSegment"/> ordinals to the
/// named slots that the directory-structure plan requires. Claude only
/// receives the redacted content and segment summaries
/// (<c>{Ordinal, Kind, Token, Length}</c>) — never the original literals.
/// </summary>
public interface IPiiSlotMappingService
{
    Task<Result<PiiSlotMap>> MapAsync(
        RedactedFileDescriptor file,
        IReadOnlyList<string> requiredSlots,
        CancellationToken ct = default);
}

/// <summary>
/// Centralizes filename / path sanitization rules used by both the
/// rehydrator and the structure-execution planner.
/// </summary>
public interface IPathSafetyService
{
    /// <summary>The character substituted for any disallowed input character.</summary>
    char ReplacementChar { get; }

    /// <summary>
    /// Returns a single path segment (no directory separators) with all
    /// platform-illegal characters, control codes, reserved names, and
    /// trailing dots/spaces handled. Length-capped to a safe segment length.
    /// </summary>
    string SanitizeSegment(string segment);

    /// <summary>
    /// Sanitizes every segment of a relative or absolute path. Preserves
    /// directory separators and any drive / UNC prefix.
    /// </summary>
    string SanitizePath(string path);
}

/// <summary>
/// Pre-execute validator that confirms every file matched by a
/// <c>DirectoryStructureRule</c> has a slot map that satisfies the rule's
/// <c>RequiredTokenSlots</c>. Files that fail validation are reported so the
/// UI can surface them before <c>ExecuteStructurePlanAsync</c> runs.
/// </summary>
public interface ISlotCompletenessValidator
{
    Task<Result<IReadOnlyList<MissingSlotReport>>> ValidateAsync(
        Guid cleaningId,
        CancellationToken ct = default);
}
