// SPDX-FileCopyrightText: 2024 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 John Space <bigdumb421@gmail.com>
// SPDX-FileCopyrightText: 2025 GreyMaria <mariomister541@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Server.Flash;
using Content.Server.Stunnable;
using Content.Shared._Goobstation.Flashbang;
using Content.Shared.Examine;
using Content.Shared.Inventory;

namespace Content.Server._Goobstation.Flashbang;

public sealed class FlashbangSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashbangComponent, AreaFlashEvent>(OnFlash);
        SubscribeLocalEvent<InventoryComponent, GetFlashbangedEvent>(
            OnInventoryFlashbanged);
        // SubscribeLocalEvent<FlashSoundSuppressionComponent, GetFlashbangedEvent>(OnFlashbanged); // funkystation: not right now
        SubscribeLocalEvent<FlashSoundSuppressionComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<FlashSoundSuppressionComponent> ent, ref ExaminedEvent args)
    {
        var range = ent.Comp.ProtectionRange;
        var message = range > 0
            ? Loc.GetString("flash-sound-suppression-examine", ("range", range))
            : Loc.GetString("flash-sound-suppression-fully-examine");

        args.PushMarkup(message);
    }

    // funkystation: if we have entities that naturally resist (without being immune to) flashbangs we can restore this and write it correctly
    // private void OnFlashbanged(Entity<FlashSoundSuppressionComponent> ent, ref GetFlashbangedEvent args)
    // {
    //     args.ProtectionRange = MathF.Min(args.ProtectionRange, ent.Comp.ProtectionRange);
    // }

    // funkystation: borrow perfectly functional code from wizden FlashSystem to make flashbang protection work
    private void OnInventoryFlashbanged(Entity<InventoryComponent> ent,
        ref GetFlashbangedEvent args)
    {
        foreach (var slot in new[] { "head", "ears" })
        {
            if (_inventory.TryGetSlotEntity(ent, slot, out var item, ent.Comp) && TryComp<FlashSoundSuppressionComponent>(item, out var bangProtector))
                args.ProtectionRange = MathF.Min(args.ProtectionRange, bangProtector.ProtectionRange);
        }
    }

    private void OnFlash(Entity<FlashbangComponent> ent, ref AreaFlashEvent args)
    {
        var comp = ent.Comp;

        if (comp is { KnockdownTime: <= 0, StunTime: <= 0 })
            return;

        var ev = new GetFlashbangedEvent(args.Range);
        RaiseLocalEvent(args.Target, ev, true);

        var protectionRange = ev.ProtectionRange;

        if (protectionRange <= 0f)
            return;

        var distance = MathF.Max(0f, args.Distance);

        if (distance > protectionRange)
            return;

        var ratio = distance / protectionRange;

        var knockdownTime = float.Lerp(comp.KnockdownTime, 0f, ratio);
        if (knockdownTime > 0f)
            _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(knockdownTime), true);

        var stunTime = float.Lerp(comp.StunTime, 0f, ratio);
        if (stunTime > 0f)
            _stun.TryStun(args.Target, TimeSpan.FromSeconds(stunTime), true);
    }
}
