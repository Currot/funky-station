// SPDX-FileCopyrightText: 2023 Alekshhh <44923899+Alekshhh@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Mining;

/// <summary>
/// This is a prototype for defining ores that generate in rock
/// </summary>
[Prototype]
public sealed partial class OrePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public EntProtoId? OreEntity;

    [DataField]
    public int MinOreYield = 1;

    [DataField]
    public int MaxOreYield = 1;

    [DataField]
    public SpriteSpecifier? OreSprite;
}
