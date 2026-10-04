//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SevenBoldPencil.MagicOpticMount;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "7Bpencil.MagicOpticMount";
    public string Name { get; init; } = "7Bpencil.MagicOpticMount";
    public string Author { get; init; } = "7Bpencil";
    public List<string>? Contributors { get; init; } = null;
    public Version Version { get; init; } = new("0.1.0");
    public Range SptVersion { get; init; } = new("~4.1.6");
    public List<string>? Incompatibilities { get; init; } = null;
    public Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~3.0.6") }
    };
    public string? Url { get; init; } = null;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}
