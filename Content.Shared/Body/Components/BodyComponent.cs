// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Jezithyr <jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 V <97265903+formlessnameless@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyComponent : Component
{
    /// <summary>
    /// Relevant template to spawn for this body.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BodyPrototype>? Prototype;

    /// <summary>
    /// Container that holds the root body part.
    /// </summary>
    /// <remarks>
    /// Typically is the torso.
    /// </remarks>
    [ViewVariables] public ContainerSlot RootContainer = default!;

    [ViewVariables]
    public string RootPartSlot => RootContainer.ID;

    [DataField, AutoNetworkedField]
    public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    /// <summary>
    /// The amount of legs required to move at full speed.
    /// If 0, then legs do not impact speed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RequiredLegs;

    [ViewVariables]
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> LegEntities = new();

    // EE edit
    /// <summary>
    /// Determines if the entity will show up on thermal scanners for thermal vision.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ThermalVisibility = true;
}
