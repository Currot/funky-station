// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Clothing that can be enabled and disabled with an action.
/// Requires <see cref="ItemToggleComponent"/>.
/// </summary>
/// <remarks>
/// Not to be confused with <see cref="ToggleableClothingComponent"/> for hardsuit helmets and such.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ToggleClothingSystem))]
public sealed partial class ToggleClothingComponent : Component
{
    /// <summary>
    /// The action to add when equipped, even if not worn.
    /// This must raise <see cref="ToggleActionEvent"/> to then get handled.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// If true, automatically disable the clothing after unequipping it.
    /// </summary>
    [DataField]
    public bool DisableOnUnequip;

    /// <summary>
    /// If true, the clothes must equip for adding action.
    /// </summary>
    [DataField]
    public bool MustEquip = true;
}

/// <summary>
/// Raised on the clothing when being equipped to see if it should add the action.
/// </summary>
[ByRefEvent]
public record struct ToggleClothingCheckEvent(EntityUid User, bool Cancelled = false);
