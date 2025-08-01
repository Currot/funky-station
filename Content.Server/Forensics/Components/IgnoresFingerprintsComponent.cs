// SPDX-FileCopyrightText: 2023 themias <89101928+themias@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Forensics.Components;

/// <summary>
/// This component is for entities we do not wish to track fingerprints/fibers, like puddles
/// </summary>
[RegisterComponent]
public sealed partial class IgnoresFingerprintsComponent : Component { }
