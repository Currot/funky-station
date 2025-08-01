// SPDX-FileCopyrightText: 2024 SlamBamActionman <83650252+SlamBamActionman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// This is used for a clothing item that hides an appearance layer.
/// The entity's HumanoidAppearance component must have the corresponding hideLayerOnEquip value.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HideLayerClothingComponent : Component
{
    /// <summary>
    /// The appearance layer to hide.
    /// </summary>
    [DataField]
    public HashSet<HumanoidVisualLayers> Slots = new();

    /// <summary>
    /// If true, the layer will only hide when the item is in a toggled state (e.g. masks)
    /// </summary>
    [DataField]
    public bool HideOnToggle = false;
}
