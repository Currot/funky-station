// SPDX-FileCopyrightText: 2025 Steve <marlumpy@gmail.com>
// SPDX-FileCopyrightText: 2025 marc-pelletier <113944176+marc-pelletier@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class HalonOxygenAbsorptionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5.0f)
            return ReactionResult.NoReaction;

        var initHalon = mixture.GetMoles(Gas.Halon);
        var initOxy = mixture.GetMoles(Gas.Oxygen);

        var temperature = mixture.Temperature;

        var heatEfficiency = Math.Min(temperature / (Atmospherics.FireMinimumTemperatureToExist * 10f), Math.Min(initHalon, initOxy / 20f));

        if (heatEfficiency <= 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Halon, -heatEfficiency);
        mixture.AdjustMoles(Gas.Oxygen, -heatEfficiency * 20f);
        mixture.AdjustMoles(Gas.CarbonDioxide, heatEfficiency * 2.5f);

        var energyReleased = heatEfficiency * Atmospherics.HalonCombustionEnergy;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}