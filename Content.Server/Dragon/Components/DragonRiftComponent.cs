// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Dragon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Dragon;

[RegisterComponent]
public sealed partial class DragonRiftComponent : SharedDragonRiftComponent
{
    /// <summary>
    /// Dragon that spawned this rift.
    /// </summary>
    [DataField("dragon")] public EntityUid? Dragon;

    /// <summary>
    /// How long the rift has been active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("accumulator")]
    public float Accumulator = 0f;

    /// <summary>
    /// The maximum amount we can accumulate before becoming impervious.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxAccumuluator")] public float MaxAccumulator = 300f;

    /// <summary>
    /// Accumulation of the spawn timer.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnAccumulator")]
    public float SpawnAccumulator = 30f;

    /// <summary>
    /// How long it takes for a new spawn to be added.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnCooldown")]
    public float SpawnCooldown = 30f;

    [ViewVariables(VVAccess.ReadWrite), DataField("spawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPrototype = "MobCarpDragon";
}
