// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.PowerCell;

[UsedImplicitly]
public sealed class PowerCellSystem : SharedPowerCellSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerCellVisualsComponent, AppearanceChangeEvent>(OnPowerCellVisualsChange);
    }

    /// <inheritdoc/>
    public override bool HasActivatableCharge(EntityUid uid, PowerCellDrawComponent? battery = null, PowerCellSlotComponent? cell = null,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref battery, ref cell, false))
            return true;

        return battery.CanUse;
    }

    /// <inheritdoc/>
    public override bool HasDrawCharge(
        EntityUid uid,
        PowerCellDrawComponent? battery = null,
        PowerCellSlotComponent? cell = null,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref battery, ref cell, false))
            return true;

        return battery.CanDraw;
    }

    private void OnPowerCellVisualsChange(EntityUid uid, PowerCellVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.TryGetLayer((int) PowerCellVisualLayers.Unshaded, out var unshadedLayer))
            return;

        if (_appearance.TryGetData<byte>(uid, PowerCellVisuals.ChargeLevel, out var level, args.Component))
        {
            if (level == 0)
            {
                unshadedLayer.Visible = false;
                return;
            }

            unshadedLayer.Visible = true;
            args.Sprite.LayerSetState(PowerCellVisualLayers.Unshaded, $"o{level}");
        }
    }

    private enum PowerCellVisualLayers : byte
    {
        Base,
        Unshaded,
    }
}
