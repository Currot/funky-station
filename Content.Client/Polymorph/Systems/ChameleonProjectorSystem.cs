// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Client.Effects;
using Content.Client.Smoking;
using Content.Shared.Chemistry.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChameleonDisguisedComponent, GetFlashEffectTargetEvent>(OnGetFlashEffectTargetEvent);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<SolutionContainerVisualsComponent>(ent);
        CopyComp<BurnStateVisualsComponent>(ent);

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    private void OnStartup(Entity<ChameleonDisguisedComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        ent.Comp.WasVisible = sprite.Visible;
        sprite.Visible = false;
    }

    private void OnShutdown(Entity<ChameleonDisguisedComponent> ent, ref ComponentShutdown args)
    {
        if (_spriteQuery.TryComp(ent, out var sprite))
            sprite.Visible = ent.Comp.WasVisible;
    }

    private void OnGetFlashEffectTargetEvent(Entity<ChameleonDisguisedComponent> ent, ref GetFlashEffectTargetEvent args)
    {
        args.Target = ent.Comp.Disguise;
    }
}
