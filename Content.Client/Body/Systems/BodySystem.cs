// SPDX-FileCopyrightText: 2022 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Body.Systems;
// Shitmed Change Start
using Content.Shared._Shitmed.Body.Part;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.Body.Components;
// Shitmed Change End

namespace Content.Client.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    // Shitmed Change Start
    [Dependency] private readonly MarkingManager _markingManager = default!;

    private void ApplyMarkingToPart(MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        bool visible,
        SpriteComponent sprite)
    {
        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            var markingSprite = markingPrototype.Sprites[j];

            if (markingSprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (!sprite.LayerMapTryGet(layerId, out _))
            {
                var layer = sprite.AddLayer(markingSprite, j + 1);
                sprite.LayerMapSet(layerId, layer);
                sprite.LayerSetSprite(layerId, rsi);
            }

            sprite.LayerSetVisible(layerId, visible);

            if (!visible)
                continue;

            // Okay so if the marking prototype is modified but we load old marking data this may no longer be valid
            // and we need to check the index is correct. So if that happens just default to white?
            if (colors != null && j < colors.Count)
                sprite.LayerSetColor(layerId, colors[j]);
            else
                sprite.LayerSetColor(layerId, Color.White);
        }
    }

    protected override void ApplyPartMarkings(EntityUid target, BodyPartAppearanceComponent component)
    {
        if (!TryComp(target, out SpriteComponent? sprite))
            return;

        if (component.Color != null)
            sprite.Color = component.Color.Value;

        foreach (var (visualLayer, markingList) in component.Markings)
            foreach (var marking in markingList)
            {
                if (!_markingManager.TryGetMarking(marking, out var markingPrototype))
                    continue;

                ApplyMarkingToPart(markingPrototype, marking.MarkingColors, marking.Visible, sprite);
            }
    }

    protected override void RemoveBodyMarkings(EntityUid target, BodyPartAppearanceComponent partAppearance, HumanoidAppearanceComponent bodyAppearance)
    {
        return;
    }
    // Shitmed Change End
}
