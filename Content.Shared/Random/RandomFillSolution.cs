// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 forthbridge <79264743+forthbridge@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Random;

/// <summary>
///     Data that specifies reagents that share the same weight and quantity for use with WeightedRandomSolution.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomFillSolution
{
    /// <summary>
    ///     Quantity of listed reagents.
    /// </summary>
    [DataField("quantity")]
    public FixedPoint2 Quantity = 0;

    /// <summary>
    ///     Random weight of listed reagents.
    /// </summary>
    [DataField("weight")]
    public float Weight = 0;

    /// <summary>
    ///     Listed reagents that the weight and quantity apply to.
    /// </summary>
    [DataField("reagents", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public List<string> Reagents = new();
}
