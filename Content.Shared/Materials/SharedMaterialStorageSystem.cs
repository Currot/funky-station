// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 PJBot <pieterjan.briers+bot@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 yglop <95057024+yglop@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Tay <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 corresp0nd <46357632+corresp0nd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Shared._Goobstation.Silo;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Research.Components;

namespace Content.Shared.Materials;

/// <summary>
/// This handles storing materials and modifying their amounts
/// <see cref="MaterialStorageComponent"/>
/// </summary>
public abstract class SharedMaterialStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedSiloSystem _silo = default!; // Goobstation

    /// <summary>
    /// Default volume for a sheet if the material's entity prototype has no material composition.
    /// </summary>
    private const int DefaultSheetVolume = 100;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialStorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MaterialStorageComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MaterialStorageComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<InsertingMaterialStorageComponent>();
        while (query.MoveNext(out var uid, out var inserting))
        {
            if (_timing.CurTime < inserting.EndTime)
                continue;

            _appearance.SetData(uid, MaterialStorageVisuals.Inserting, false);
            RemComp(uid, inserting);
        }
    }

    private void OnMapInit(EntityUid uid, MaterialStorageComponent component, MapInitEvent args)
    {
        _appearance.SetData(uid, MaterialStorageVisuals.Inserting, false);
    }

    /// <summary>
    /// Gets the volume of a specified material contained in this storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="material"></param>
    /// <param name="component"></param>
    /// <returns>The volume of the material</returns>
    [PublicAPI]
    public int GetMaterialAmount(EntityUid uid, MaterialPrototype material, MaterialStorageComponent? component = null)
    {
        return GetMaterialAmount(uid, material.ID, component);
    }

    /// <summary>
    /// Gets the volume of a specified material contained in this storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="material"></param>
    /// <param name="component"></param>
    /// <returns>The volume of the material</returns>
    public int GetMaterialAmount(EntityUid uid, string material, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0; //you have nothing
        if (component.ConnectToSilo && _silo.TryGetMaterialAmount(uid, material, out var amount)) // Goobstation
            return amount;
        return component.Storage.GetValueOrDefault(material, 0);
    }

    /// <summary>
    /// Gets the total volume of all materials in the storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>The volume of all materials in the storage</returns>
    public int GetTotalMaterialAmount(EntityUid uid, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;
        if (component.ConnectToSilo && _silo.TryGetTotalMaterialAmount(uid, out var amount)) // Goobstation
            return amount;
        return component.Storage.Values.Sum();
    }

    /// <summary>
    /// Tests if a specific amount of volume will fit in the storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <returns>If the specified volume will fit</returns>
    public bool CanTakeVolume(EntityUid uid, int volume, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        return component.StorageLimit == null || GetTotalMaterialAmount(uid, component) + volume <= component.StorageLimit;
    }

    /// <summary>
    /// Checks if the specified material can be changed by the specified volume.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="materialId"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <returns>If the amount can be changed</returns>
    public bool CanChangeMaterialAmount(EntityUid uid, string materialId, int volume, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanTakeVolume(uid, volume, component))
            return false;

        if (component.MaterialWhiteList == null ? false : !component.MaterialWhiteList.Contains(materialId))
            return false;

        if (component.ConnectToSilo && _silo.TryGetMaterialAmount(uid, materialId, out var siloAmount)) // Goobstation
            return siloAmount + volume >= 0;

        var amount = component.Storage.GetValueOrDefault(materialId);
        return amount + volume >= 0;
    }

    /// <summary>
    /// Checks if the specified materials can be changed by the specified volumes.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="materials"></param>
    /// <returns>If the amount can be changed</returns>
    public bool CanChangeMaterialAmount(Entity<MaterialStorageComponent?> entity, Dictionary<string,int> materials)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        foreach (var (material, amount) in materials)
        {
            if (!CanChangeMaterialAmount(entity, material, amount, entity.Comp))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Changes the amount of a specific material in the storage.
    /// Still respects the filters in place.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="materialId"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <param name="dirty"></param>
    /// <returns>If it was successful</returns>
    public bool TryChangeMaterialAmount(EntityUid uid, string materialId, int volume, MaterialStorageComponent? component = null, bool dirty = true)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (!CanChangeMaterialAmount(uid, materialId, volume, component))
            return false;

        // Goob start
        EntityUid storageUid;
        Dictionary<ProtoId<MaterialPrototype>, int> storage;
        if (component.ConnectToSilo)
        {
            var silo = _silo.GetSilo(uid);
            if (dirty && silo != null)
                Dirty(silo.Value);
            storage = silo != null ? silo.Value.Comp.Storage : component.Storage;
            storageUid = silo != null ? silo.Value : uid;
        }
        else
        {
            storage = component.Storage;
            storageUid = uid;
        }

        var existing = storage.GetOrNew(materialId);
        // Goob end

        existing += volume;

        if (existing == 0)
            storage.Remove(materialId); // Goob edit
        else
            storage[materialId] = existing; // Goob edit

        var ev = new MaterialAmountChangedEvent();
        RaiseLocalEvent(storageUid, ref ev); // Goob edit

        if (dirty)
            Dirty(uid, component);
        return true;
    }

    /// <summary>
    /// Changes the amount of a specific material in the storage.
    /// Still respects the filters in place.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="materials"></param>
    /// <returns>If the amount can be changed</returns>
    public bool TryChangeMaterialAmount(Entity<MaterialStorageComponent?> entity, Dictionary<string,int> materials)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanChangeMaterialAmount(entity, materials))
            return false;

        foreach (var (material, amount) in materials)
        {
            if (!TryChangeMaterialAmount(entity, material, amount, entity.Comp, false))
                return false;
        }

        if (entity.Comp.ConnectToSilo) // Goobstation
            _silo.DirtySilo(entity);
        Dirty(entity, entity.Comp);
        return true;
    }

    /// <summary>
    /// Tries to set the amount of material in the storage to a specific value.
    /// Still respects the filters in place.
    /// </summary>
    /// <param name="uid">The entity to change the material storage on.</param>
    /// <param name="materialId">The ID of the material to change.</param>
    /// <param name="volume">The stored material volume to set the storage to.</param>
    /// <param name="component">The storage component on <paramref name="uid"/>. Resolved automatically if not given.</param>
    /// <returns>True if it was successful (enough space etc).</returns>
    public bool TrySetMaterialAmount(
        EntityUid uid,
        string materialId,
        int volume,
        MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var curAmount = GetMaterialAmount(uid, materialId, component);
        var delta = volume - curAmount;
        return TryChangeMaterialAmount(uid, materialId, delta, component);
    }

    /// <summary>
    /// Tries to insert an entity into the material storage.
    /// </summary>
    public virtual bool TryInsertMaterialEntity(EntityUid user,
        EntityUid toInsert,
        EntityUid receiver,
        MaterialStorageComponent? storage = null,
        MaterialComponent? material = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(receiver, ref storage))
            return false;

        if (!Resolve(toInsert, ref material, ref composition, false))
            return false;

        if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, toInsert))
            return false;

        if (HasComp<UnremoveableComponent>(toInsert))
            return false;

        // Material Whitelist checked implicitly by CanChangeMaterialAmount();

        var multiplier = TryComp<StackComponent>(toInsert, out var stackComponent) ? stackComponent.Count : 1;
        var totalVolume = 0;
        foreach (var (mat, vol) in composition.MaterialComposition)
        {
            if (!CanChangeMaterialAmount(receiver, mat, vol * multiplier, storage))
                return false;
            totalVolume += vol * multiplier;
        }

        if (!CanTakeVolume(receiver, totalVolume, storage))
            return false;

        foreach (var (mat, vol) in composition.MaterialComposition)
        {
            TryChangeMaterialAmount(receiver, mat, vol * multiplier, storage);
        }

        var insertingComp = EnsureComp<InsertingMaterialStorageComponent>(receiver);
        insertingComp.EndTime = _timing.CurTime + storage.InsertionTime;
        if (!storage.IgnoreColor)
        {
            _prototype.TryIndex<MaterialPrototype>(composition.MaterialComposition.Keys.First(), out var lastMat);
            insertingComp.MaterialColor = lastMat?.Color;
        }
        _appearance.SetData(receiver, MaterialStorageVisuals.Inserting, true);
        Dirty(receiver, insertingComp);

        var ev = new MaterialEntityInsertedEvent(material);
        RaiseLocalEvent(receiver, ref ev);
        return true;
    }

    /// <summary>
    /// Broadcasts an event that will collect a list of which materials
    /// are allowed to be inserted into the materialStorage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void UpdateMaterialWhitelist(EntityUid uid, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;
        var ev = new GetMaterialWhitelistEvent(uid);
        RaiseLocalEvent(uid, ref ev);
        component.MaterialWhiteList = ev.Whitelist;
        Dirty(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, MaterialStorageComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !component.InsertOnInteract)
            return;
        args.Handled = TryInsertMaterialEntity(args.User, args.Used, uid, component);
    }

    private void OnDatabaseModified(Entity<MaterialStorageComponent> ent, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateMaterialWhitelist(ent);
    }

    public int GetSheetVolume(MaterialPrototype material)
    {
        if (material.StackEntity == null)
            return DefaultSheetVolume;

        var proto = _prototype.Index<EntityPrototype>(material.StackEntity);

        if (!proto.TryGetComponent<PhysicalCompositionComponent>(out var composition, EntityManager.ComponentFactory))
            return DefaultSheetVolume;

        return composition.MaterialComposition.FirstOrDefault(kvp => kvp.Key == material.ID).Value;
    }
}
