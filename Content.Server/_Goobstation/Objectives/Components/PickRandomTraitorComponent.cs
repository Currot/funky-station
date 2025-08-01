// SPDX-FileCopyrightText: 2024 Mary <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Server.Objectives.Components;

namespace Content.Server._Goobstation.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random traitor
/// If there are no traitors it will fallback to any person.
/// </summary>
[RegisterComponent]
public sealed partial class PickRandomTraitorComponent : Component
{
}
