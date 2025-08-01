// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Jezithyr <jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 0x6273 <0x40@keemail.me>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2024 John Space <bigdumb421@gmail.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 corresp0nd <46357632+corresp0nd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

// Shitmed Change Start
using Content.Shared._Shitmed.Body.Events;
using Content.Shared._Shitmed.Body.Part;
using Content.Shared._Shitmed.BodyEffects;
using Content.Shared._Shitmed.Targeting.Events;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Random;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!; // Shitmed Change
    [Dependency] private readonly InventorySystem _inventorySystem = default!; // Shitmed Change

    private void InitializeParts()
    {
        // TODO: This doesn't handle comp removal on child ents.

        // If you modify this also see the Body partial for root parts.
        SubscribeLocalEvent<BodyPartComponent, EntInsertedIntoContainerMessage>(OnBodyPartInserted);
        SubscribeLocalEvent<BodyPartComponent, EntRemovedFromContainerMessage>(OnBodyPartRemoved);

        // Shitmed Change Start
        SubscribeLocalEvent<BodyPartComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BodyPartComponent, ComponentRemove>(OnBodyPartRemove);
        SubscribeLocalEvent<BodyPartComponent, AmputateAttemptEvent>(OnAmputateAttempt);
        SubscribeLocalEvent<BodyPartComponent, BodyPartEnableChangedEvent>(OnPartEnableChanged);
    }

    private void OnMapInit(Entity<BodyPartComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.PartType == BodyPartType.Torso)
        {
            // For whatever reason this slot is initialized properly on the server, but not on the client.
            // This seems to be an issue due to wiz-merge, on my old branch it was properly instantiating
            // ItemInsertionSlot's container on both ends. It does show up properly on ItemSlotsComponent though.
            _slots.AddItemSlot(ent, ent.Comp.ContainerName, ent.Comp.ItemInsertionSlot);
            Dirty(ent, ent.Comp);
        }

        if (ent.Comp.OnAdd is not null || ent.Comp.OnRemove is not null)
            EnsureComp<BodyPartEffectComponent>(ent);

        foreach (var connection in ent.Comp.Children.Keys)
        {
            Containers.EnsureContainer<ContainerSlot>(ent, GetPartSlotContainerId(connection));
        }
    }

    private void OnBodyPartRemove(Entity<BodyPartComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.PartType == BodyPartType.Torso)
            _slots.RemoveItemSlot(ent, ent.Comp.ItemInsertionSlot);
    }

    private void OnPartEnableChanged(Entity<BodyPartComponent> partEnt, ref BodyPartEnableChangedEvent args)
    {
        if (!partEnt.Comp.CanEnable && args.Enabled)
            return;

        partEnt.Comp.Enabled = args.Enabled;

        if (args.Enabled)
        {
            EnablePart(partEnt);
            if (partEnt.Comp.Body is { Valid: true } bodyEnt)
                RaiseLocalEvent(partEnt, new BodyPartComponentsModifyEvent(bodyEnt, true));
        }
        else
        {
            DisablePart(partEnt);
            if (partEnt.Comp.Body is { Valid: true } bodyEnt)
                RaiseLocalEvent(partEnt, new BodyPartComponentsModifyEvent(bodyEnt, false));
        }

        Dirty(partEnt, partEnt.Comp);
    }

    private void EnablePart(Entity<BodyPartComponent> partEnt)
    {
        if (!TryComp(partEnt.Comp.Body, out BodyComponent? body))
            return;

        // I hate having to hardcode these checks so much.
        if (partEnt.Comp.PartType == BodyPartType.Leg)
            AddLeg(partEnt, (partEnt.Comp.Body.Value, body));

        if (partEnt.Comp.PartType == BodyPartType.Arm)
        {
            var hand = GetBodyChildrenOfType(partEnt.Comp.Body.Value, BodyPartType.Hand, symmetry: partEnt.Comp.Symmetry).FirstOrDefault();
            if (hand != default)
            {
                var ev = new BodyPartEnabledEvent(hand);
                RaiseLocalEvent(partEnt.Comp.Body.Value, ref ev);
            }
        }

        if (partEnt.Comp.PartType == BodyPartType.Hand)
        {
            var ev = new BodyPartEnabledEvent(partEnt);
            RaiseLocalEvent(partEnt.Comp.Body.Value, ref ev);
        }
    }

    /// <summary>
    ///     Shitmed Change: This function handles dropping the items in an entity's slots if they lose all of a given part.
    ///     Such as their hands, feet, head, etc.
    /// </summary>
    public void DropSlotContents(Entity<BodyPartComponent> partEnt)
    {
        if (partEnt.Comp.Body is not null
            && TryComp<InventoryComponent>(partEnt.Comp.Body, out var inventory) // Prevent error for non-humanoids
            && GetBodyPartCount(partEnt.Comp.Body.Value, partEnt.Comp.PartType) == 1
            && TryGetPartSlotContainerName(partEnt.Comp.PartType, out var containerNames))
        {
            foreach (var containerName in containerNames)
                _inventorySystem.DropSlotContents(partEnt.Comp.Body.Value, containerName, inventory);
        }

    }

    private void DisablePart(Entity<BodyPartComponent> partEnt)
    {
        if (!TryComp(partEnt.Comp.Body, out BodyComponent? body))
            return;

        if (partEnt.Comp.PartType == BodyPartType.Leg)
            RemoveLeg(partEnt, (partEnt.Comp.Body.Value, body));

        if (partEnt.Comp.PartType == BodyPartType.Arm)
        {
            var hand = GetBodyChildrenOfType(partEnt.Comp.Body.Value, BodyPartType.Hand, symmetry: partEnt.Comp.Symmetry).FirstOrDefault();
            if (hand != default)
            {
                var ev = new BodyPartDisabledEvent(hand);
                RaiseLocalEvent(partEnt.Comp.Body.Value, ref ev);
            }
        }

        if (partEnt.Comp.PartType == BodyPartType.Hand)
        {
            var ev = new BodyPartDisabledEvent(partEnt);
            RaiseLocalEvent(partEnt.Comp.Body.Value, ref ev);
        }
    }

    // TODO: Refactor this crap. I hate it so much.
    private void RemovePartEffect(Entity<BodyPartComponent> partEnt, Entity<BodyComponent?> bodyEnt)
    {
        if (TerminatingOrDeleted(bodyEnt)
            || !Resolve(bodyEnt, ref bodyEnt.Comp, logMissing: false))
            return;

        RemovePartChildren(partEnt, bodyEnt, bodyEnt.Comp);
    }

    protected void RemovePartChildren(Entity<BodyPartComponent> partEnt, EntityUid bodyEnt, BodyComponent? body = null)
    {
        if (!Resolve(bodyEnt, ref body, logMissing: false))
            return;

        if (partEnt.Comp.Children.Any())
        {
            foreach (var slotId in partEnt.Comp.Children.Keys)
            {
                if (Containers.TryGetContainer(partEnt, GetPartSlotContainerId(slotId), out var container) &&
                    container is ContainerSlot slot &&
                    slot.ContainedEntity is { } childEntity &&
                    TryComp(childEntity, out BodyPartComponent? childPart))
                {
                    var ev = new BodyPartEnableChangedEvent(false);
                    RaiseLocalEvent(childEntity, ref ev);
                    DropPart((childEntity, childPart));
                }
            }

            Dirty(bodyEnt, body);
        }
    }

    protected virtual void DropPart(Entity<BodyPartComponent> partEnt)
    {
        DropSlotContents(partEnt);
        // I don't know if this can cause issues, since any part that's being detached HAS to have a Body.
        // though I really just want the compiler to shut the fuck up.
        var body = partEnt.Comp.Body.GetValueOrDefault();
        if (TryComp(partEnt, out TransformComponent? transform) && _gameTiming.IsFirstTimePredicted)
        {
            var enableEvent = new BodyPartEnableChangedEvent(false);
            RaiseLocalEvent(partEnt, ref enableEvent);
            var droppedEvent = new BodyPartDroppedEvent(partEnt);
            RaiseLocalEvent(body, ref droppedEvent);
            SharedTransform.AttachToGridOrMap(partEnt, transform);
            _randomHelper.RandomOffset(partEnt, 0.5f);
        }

    }

    private void OnAmputateAttempt(Entity<BodyPartComponent> partEnt, ref AmputateAttemptEvent args) =>
        DropPart(partEnt);

    // Shitmed Change End
    private void OnBodyPartInserted(Entity<BodyPartComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Body part inserted into another body part.
        var insertedUid = args.Entity;
        var slotId = args.Container.ID;

        if (ent.Comp.Body is null)
            return;

        if (TryComp(insertedUid, out BodyPartComponent? part) && slotId.Contains(PartSlotContainerIdPrefix + GetSlotFromBodyPart(part))) // Shitmed Change
        {
            AddPart(ent.Comp.Body.Value, (insertedUid, part), slotId);
            RecursiveBodyUpdate((insertedUid, part), ent.Comp.Body.Value);
            CheckBodyPart((insertedUid, part), GetTargetBodyPart(part), false); // Shitmed Change
        }

        if (TryComp(insertedUid, out OrganComponent? organ) && slotId.Contains(OrganSlotContainerIdPrefix + organ.SlotId)) // Shitmed Change
        {
            AddOrgan((insertedUid, organ), ent.Comp.Body.Value, ent);
        }
    }

    private void OnBodyPartRemoved(Entity<BodyPartComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Body part removed from another body part.
        var removedUid = args.Entity;
        var slotId = args.Container.ID;

        // Shitmed Change Start
        if (TryComp(removedUid, out BodyPartComponent? part))
        {
            if (!slotId.Contains(PartSlotContainerIdPrefix + GetSlotFromBodyPart(part)))
                return;

            DebugTools.Assert(part.Body == ent.Comp.Body);

            if (part.Body is not null)
            {
                CheckBodyPart((removedUid, part), GetTargetBodyPart(part), true);
                RemovePart(part.Body.Value, (removedUid, part), slotId);
                RecursiveBodyUpdate((removedUid, part), null);
            }
        }

        if (TryComp(removedUid, out OrganComponent? organ))
        {
            if (!slotId.Contains(OrganSlotContainerIdPrefix + organ.SlotId))
                return;

            DebugTools.Assert(organ.Body == ent.Comp.Body);

            RemoveOrgan((removedUid, organ), ent);
        }
        // Shitmed Change End
    }

    private void RecursiveBodyUpdate(Entity<BodyPartComponent> ent, EntityUid? bodyUid)
    {
        ent.Comp.Body = bodyUid;
        Dirty(ent, ent.Comp);

        foreach (var slotId in ent.Comp.Organs.Keys)
        {
            if (!Containers.TryGetContainer(ent, GetOrganContainerId(slotId), out var container))
                continue;

            foreach (var organ in container.ContainedEntities)
            {
                if (!TryComp(organ, out OrganComponent? organComp))
                    continue;

                Dirty(organ, organComp);

                if (organComp.Body is { Valid: true } oldBodyUid)
                {
                    var removedEv = new OrganRemovedFromBodyEvent(oldBodyUid, ent);
                    RaiseLocalEvent(organ, ref removedEv);
                }

                organComp.Body = bodyUid;
                if (bodyUid is not null)
                {
                    var addedEv = new OrganAddedToBodyEvent(bodyUid.Value, ent);
                    RaiseLocalEvent(organ, ref addedEv);
                }
            }
        }

        // The code for RemovePartEffect() should live here, because it literally is the point of this recursive function.
        // But the debug asserts at the top plus existing tests need refactoring for this. So we'll be lazy.
        foreach (var slotId in ent.Comp.Children.Keys)
        {
            if (!Containers.TryGetContainer(ent, GetPartSlotContainerId(slotId), out var container))
                continue;

            foreach (var containedUid in container.ContainedEntities)
            {
                if (TryComp(containedUid, out BodyPartComponent? childPart))
                    RecursiveBodyUpdate((containedUid, childPart), bodyUid);
            }
        }
    }

    protected virtual void AddPart(
        Entity<BodyComponent?> bodyEnt,
        Entity<BodyPartComponent> partEnt,
        string slotId)
    {
        Dirty(partEnt, partEnt.Comp);
        partEnt.Comp.Body = bodyEnt;

        if (partEnt.Comp.Enabled && partEnt.Comp.Body is { Valid: true } body) // Shitmed Change
            RaiseLocalEvent(partEnt, new BodyPartComponentsModifyEvent(body, true));

        var ev = new BodyPartAddedEvent(slotId, partEnt);
        RaiseLocalEvent(bodyEnt, ref ev);

        AddLeg(partEnt, bodyEnt);
    }

    protected virtual void RemovePart(
        Entity<BodyComponent?> bodyEnt,
        Entity<BodyPartComponent> partEnt,
        string slotId)
    {
        Resolve(bodyEnt, ref bodyEnt.Comp, logMissing: false);
        Dirty(partEnt, partEnt.Comp);

        // Shitmed Change Start
        if (partEnt.Comp.Body is { Valid: true } body)
            RaiseLocalEvent(partEnt, new BodyPartComponentsModifyEvent(body, false));
        partEnt.Comp.ParentSlot = null;
        // Shitmed Change End

        var ev = new BodyPartRemovedEvent(slotId, partEnt);
        RaiseLocalEvent(bodyEnt, ref ev);

        RemoveLeg(partEnt, bodyEnt);
        RemovePartEffect(partEnt, bodyEnt); // Shitmed Change
        PartRemoveDamage(bodyEnt, partEnt);
    }

    private void AddLeg(Entity<BodyPartComponent> legEnt, Entity<BodyComponent?> bodyEnt)
    {
        if (!Resolve(bodyEnt, ref bodyEnt.Comp, logMissing: false))
            return;

        if (legEnt.Comp.PartType == BodyPartType.Leg)
        {
            bodyEnt.Comp.LegEntities.Add(legEnt);
            UpdateMovementSpeed(bodyEnt);
            Dirty(bodyEnt, bodyEnt.Comp);
        }
    }

    private void RemoveLeg(Entity<BodyPartComponent> legEnt, Entity<BodyComponent?> bodyEnt)
    {
        if (!Resolve(bodyEnt, ref bodyEnt.Comp, logMissing: false))
            return;

        if (legEnt.Comp.PartType == BodyPartType.Leg)
        {
            bodyEnt.Comp.LegEntities.Remove(legEnt);
            UpdateMovementSpeed(bodyEnt);
            Dirty(bodyEnt, bodyEnt.Comp);
            Standing.Down(bodyEnt); // Shitmed Change
        }
    }

    // Shitmed Change: made virtual, bleeding damage is done on server
    protected virtual void PartRemoveDamage(Entity<BodyComponent?> bodyEnt, Entity<BodyPartComponent> partEnt)
    {
    }

    /// <summary>
    /// Tries to get the parent body part to this if applicable.
    /// Doesn't validate if it's a part of body system.
    /// </summary>
    public EntityUid? GetParentPartOrNull(EntityUid uid)
    {
        if (!Containers.TryGetContainingContainer((uid, null, null), out var container))
            return null;

        var parent = container.Owner;

        if (!HasComp<BodyPartComponent>(parent))
            return null;

        return parent;
    }

    /// <summary>
    /// Tries to get the parent body part and slot to this if applicable.
    /// </summary>
    public (EntityUid Parent, string Slot)? GetParentPartAndSlotOrNull(EntityUid uid)
    {
        if (!Containers.TryGetContainingContainer((uid, null, null), out var container))
            return null;

        var slotId = GetPartSlotContainerIdFromContainer(container.ID);

        if (string.IsNullOrEmpty(slotId))
            return null;

        var parent = container.Owner;

        if (!TryComp<BodyPartComponent>(parent, out var parentBody)
            || !parentBody.Children.ContainsKey(slotId))
            return null;

        return (parent, slotId);
    }

    /// <summary>
    /// Tries to get the relevant parent body part to this if it exists.
    /// It won't exist if this is the root body part or if it's not in a body.
    /// </summary>
    public bool TryGetParentBodyPart(
        EntityUid partUid,
        [NotNullWhen(true)] out EntityUid? parentUid,
        [NotNullWhen(true)] out BodyPartComponent? parentComponent)
    {
        DebugTools.Assert(HasComp<BodyPartComponent>(partUid));
        parentUid = null;
        parentComponent = null;

        if (Containers.TryGetContainingContainer((partUid, null, null), out var container) &&
            TryComp(container.Owner, out parentComponent))
        {
            parentUid = container.Owner;
            return true;
        }

        return false;
    }

    #region Slots

    /// <summary>
    /// Creates a BodyPartSlot on the specified partUid.
    /// </summary>
    private BodyPartSlot? CreatePartSlot(
        EntityUid partUid,
        string slotId,
        BodyPartType partType,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partUid, ref part, logMissing: false))
            return null;

        Containers.EnsureContainer<ContainerSlot>(partUid, GetPartSlotContainerId(slotId));
        var partSlot = new BodyPartSlot(slotId, partType);
        part.Children.Add(slotId, partSlot);
        Dirty(partUid, part);
        return partSlot;
    }

    /// <summary>
    /// Tries to create a BodyPartSlot on the specified partUid.
    /// </summary>
    /// <returns>false if not relevant or can't add it.</returns>
    public bool TryCreatePartSlot(
        EntityUid? partId,
        string slotId,
        BodyPartType partType,
        [NotNullWhen(true)] out BodyPartSlot? slot,
        BodyPartComponent? part = null)
    {
        slot = null;

        if (partId is null
            || !Resolve(partId.Value, ref part, logMissing: false))
        {
            return false;
        }

        Containers.EnsureContainer<ContainerSlot>(partId.Value, GetPartSlotContainerId(slotId));
        slot = new BodyPartSlot(slotId, partType);

        if (!part.Children.ContainsKey(slotId) // Shitmed Change
            && !part.Children.TryAdd(slotId, slot.Value))
            return false;

        Dirty(partId.Value, part);
        return true;
    }

    public bool TryCreatePartSlotAndAttach(
        EntityUid parentId,
        string slotId,
        EntityUid childId,
        BodyPartType partType,
        BodyPartComponent? parent = null,
        BodyPartComponent? child = null)
    {
        return TryCreatePartSlot(parentId, slotId, partType, out _, parent)
               && AttachPart(parentId, slotId, childId, parent, child);
    }

    #endregion

    #region RootPartManagement

    /// <summary>
    /// Returns true if the partId is the root body container for the specified bodyId.
    /// </summary>
    public bool IsPartRoot(
        EntityUid bodyId,
        EntityUid partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part)
            && Resolve(bodyId, ref body)
            && Containers.TryGetContainingContainer(bodyId, partId, out var container)
            && container.ID == BodyRootContainerId;
    }

    /// <summary>
    /// Returns true if we can attach the partId to the bodyId as the root entity.
    /// </summary>
    public bool CanAttachToRoot(
        EntityUid bodyId,
        EntityUid partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        return Resolve(bodyId, ref body)
            && Resolve(partId, ref part)
            && body.RootContainer.ContainedEntity is null
            && bodyId != part.Body;
    }

    /// <summary>
    /// Returns the root part of this body if it exists.
    /// </summary>
    public (EntityUid Entity, BodyPartComponent BodyPart)? GetRootPartOrNull(EntityUid bodyId, BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body)
            || body.RootContainer.ContainedEntity is null)
        {
            return null;
        }

        return (body.RootContainer.ContainedEntity.Value,
            Comp<BodyPartComponent>(body.RootContainer.ContainedEntity.Value));
    }

    /// <summary>
    /// Returns true if the partId can be attached to the parentId in the specified slot.
    /// </summary>
    public bool CanAttachPart(
        EntityUid parentId,
        BodyPartSlot slot,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part, logMissing: false)
            && Resolve(parentId, ref parentPart, logMissing: false)
            && CanAttachPart(parentId, slot.Id, partId, parentPart, part);
    }

    /// <summary>
    /// Returns true if we can attach the specified partId to the parentId in the specified slot.
    /// </summary>
    public bool CanAttachPart(
        EntityUid parentId,
        string slotId,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part, logMissing: false)
            && Resolve(parentId, ref parentPart, logMissing: false)
            && parentPart.Children.TryGetValue(slotId, out var parentSlotData)
            && part.PartType == parentSlotData.Type
            && Containers.TryGetContainer(parentId, GetPartSlotContainerId(slotId), out var container)
            && Containers.CanInsert(partId, container);
    }

    /// <summary>
    /// Shitmed Change: Returns true if this parentId supports attaching a new part to the specified slot.
    /// </summary>
    public bool CanAttachToSlot(
        EntityUid parentId,
        string slotId,
        BodyPartComponent? parentPart = null)
    {
        return Resolve(parentId, ref parentPart, logMissing: false)
            && parentPart.Children.ContainsKey(slotId);
    }

    public bool AttachPartToRoot(
        EntityUid bodyId,
        EntityUid partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        return Resolve(bodyId, ref body)
            && Resolve(partId, ref part)
            && CanAttachToRoot(bodyId, partId, body, part)
            && Containers.Insert(partId, body.RootContainer);
    }

    #endregion

    #region Attach/Detach

    /// <summary>
    /// Attaches a body part to the specified body part parent.
    /// </summary>
    public bool AttachPart(
        EntityUid parentPartId,
        string slotId,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        return Resolve(parentPartId, ref parentPart, logMissing: false)
            && parentPart.Children.TryGetValue(slotId, out var slot)
            && AttachPart(parentPartId, slot, partId, parentPart, part);
    }

    /// <summary>
    /// Attaches a body part to the specified body part parent.
    /// </summary>
    public bool AttachPart(
        EntityUid parentPartId,
        BodyPartSlot slot,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(parentPartId, ref parentPart, logMissing: false)
            || !Resolve(partId, ref part, logMissing: false)
            || !CanAttachPart(parentPartId, slot.Id, partId, parentPart, part)
            || !parentPart.Children.ContainsKey(slot.Id))
        {
            return false;
        }

        if (!Containers.TryGetContainer(parentPartId, GetPartSlotContainerId(slot.Id), out var container))
        {
            DebugTools.Assert($"Unable to find body slot {slot.Id} for {ToPrettyString(parentPartId)}");
            return false;
        }

        part.ParentSlot = slot;

        if (TryComp(parentPart.Body, out HumanoidAppearanceComponent? bodyAppearance)
            && !HasComp<BodyPartAppearanceComponent>(partId)
            && !TerminatingOrDeleted(parentPartId)
            && !TerminatingOrDeleted(partId)) // Saw some exceptions involving these due to the spawn menu.
            EnsureComp<BodyPartAppearanceComponent>(partId);

        return Containers.Insert(partId, container);
    }

    #endregion

    #region Misc

    public void UpdateMovementSpeed(
        EntityUid bodyId,
        BodyComponent? body = null,
        MovementSpeedModifierComponent? movement = null)
    {
        if (!Resolve(bodyId, ref body, ref movement, logMissing: false)
            || body.RequiredLegs <= 0)
        {
            return;
        }

        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;
        foreach (var legEntity in body.LegEntities)
        {
            if (!TryComp<MovementBodyPartComponent>(legEntity, out var legModifier))
                continue;

            walkSpeed += legModifier.WalkSpeed;
            sprintSpeed += legModifier.SprintSpeed;
            acceleration += legModifier.Acceleration;
        }
        walkSpeed /= body.RequiredLegs;
        sprintSpeed /= body.RequiredLegs;
        acceleration /= body.RequiredLegs;
        Movement.ChangeBaseSpeed(bodyId, walkSpeed, sprintSpeed, acceleration, movement);
    }

    #endregion

    #region Queries

    /// <summary>
    /// Get all organs for the specified body part.
    /// </summary>
    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetPartOrgans(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        foreach (var slotId in part.Organs.Keys)
        {
            var containerSlotId = GetOrganContainerId(slotId);

            if (!Containers.TryGetContainer(partId, containerSlotId, out var container))
                continue;

            foreach (var containedEnt in container.ContainedEntities)
            {
                if (!TryComp(containedEnt, out OrganComponent? organ))
                    continue;

                yield return (containedEnt, organ);
            }
        }
    }

    /// <summary>
    /// Gets all BaseContainers for body parts on this entity and its child entities.
    /// </summary>
    public IEnumerable<BaseContainer> GetPartContainers(EntityUid id, BodyPartComponent? part = null)
    {
        if (!Resolve(id, ref part, logMissing: false) ||
            part.Children.Count == 0)
        {
            yield break;
        }

        foreach (var slotId in part.Children.Keys)
        {
            var containerSlotId = GetPartSlotContainerId(slotId);

            if (!Containers.TryGetContainer(id, containerSlotId, out var container))
                continue;

            yield return container;

            foreach (var ent in container.ContainedEntities)
            {
                foreach (var childContainer in GetPartContainers(ent))
                {
                    yield return childContainer;
                }
            }
        }
    }

    /// <summary>
    /// Returns all body part components for this entity including itself.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyPartChildren(
        EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        yield return (partId, part);

        foreach (var slotId in part.Children.Keys)
        {
            var containerSlotId = GetPartSlotContainerId(slotId);

            if (Containers.TryGetContainer(partId, containerSlotId, out var container))
            {
                foreach (var containedEnt in container.ContainedEntities)
                {
                    if (!TryComp(containedEnt, out BodyPartComponent? childPart))
                        continue;

                    foreach (var value in GetBodyPartChildren(containedEnt, childPart))
                    {
                        yield return value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns all body part slots for this entity.
    /// </summary>
    public IEnumerable<BodyPartSlot> GetAllBodyPartSlots(
        EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        foreach (var (slotId, slot) in part.Children)
        {
            yield return slot;

            var containerSlotId = GetOrganContainerId(slotId);

            if (Containers.TryGetContainer(partId, containerSlotId, out var container))
            {
                foreach (var containedEnt in container.ContainedEntities)
                {
                    if (!TryComp(containedEnt, out BodyPartComponent? childPart))
                        continue;

                    foreach (var subSlot in GetAllBodyPartSlots(containedEnt, childPart))
                    {
                        yield return subSlot;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns true if the bodyId has any parts of this type.
    /// </summary>
    public bool BodyHasPartType(
        EntityUid bodyId,
        BodyPartType type,
        BodyComponent? body = null)
    {
        return GetBodyChildrenOfType(bodyId, type, body).Any();
    }

    /// <summary>
    /// Returns true if the parentId has the specified childId.
    /// </summary>
    public bool PartHasChild(
        EntityUid parentId,
        EntityUid childId,
        BodyPartComponent? parent,
        BodyPartComponent? child)
    {
        if (!Resolve(parentId, ref parent, logMissing: false)
            || !Resolve(childId, ref child, logMissing: false))
        {
            return false;
        }

        foreach (var (foundId, _) in GetBodyPartChildren(parentId, parent))
        {
            if (foundId == childId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the bodyId has the specified partId.
    /// </summary>
    public bool BodyHasChild(
        EntityUid bodyId,
        EntityUid partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        return Resolve(bodyId, ref body, logMissing: false)
            && body.RootContainer.ContainedEntity is not null
            && Resolve(partId, ref part, logMissing: false)
            && TryComp(body.RootContainer.ContainedEntity, out BodyPartComponent? rootPart)
            && PartHasChild(body.RootContainer.ContainedEntity.Value, partId, rootPart, part);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildrenOfType(
        EntityUid bodyId,
        BodyPartType type,
        BodyComponent? body = null,
        // Shitmed Change
        BodyPartSymmetry? symmetry = null)
    {
        foreach (var part in GetBodyChildren(bodyId, body))
        {
            if (part.Component.PartType == type && (symmetry == null || part.Component.Symmetry == symmetry)) // Shitmed Change
                yield return part;
        }
    }

    /// <summary>
    ///     Returns a list of ValueTuples of <see cref="T"/> and OrganComponent on each organ
    ///     in the given part.
    /// </summary>
    /// <param name="uid">The part entity id to check on.</param>
    /// <param name="part">The part to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public List<(T Comp, OrganComponent Organ)> GetBodyPartOrganComponents<T>(
        EntityUid uid,
        BodyPartComponent? part = null)
        where T : IComponent
    {
        if (!Resolve(uid, ref part))
            return new List<(T Comp, OrganComponent Organ)>();

        var query = GetEntityQuery<T>();
        var list = new List<(T Comp, OrganComponent Organ)>();

        foreach (var organ in GetPartOrgans(uid, part))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((comp, organ.Component));
        }

        return list;
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and OrganComponent on each organs
    ///     in the given part.
    /// </summary>
    /// <param name="uid">The part entity id to check on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="part">The part to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetBodyPartOrganComponents<T>(
        EntityUid uid,
        [NotNullWhen(true)] out List<(T Comp, OrganComponent Organ)>? comps,
        BodyPartComponent? part = null)
        where T : IComponent
    {
        if (!Resolve(uid, ref part))
        {
            comps = null;
            return false;
        }

        comps = GetBodyPartOrganComponents<T>(uid, part);

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }

    // Shitmed Change Start
    /// <summary>
    ///     Tries to get a list of ValueTuples of EntityUid and OrganComponent on each organ
    ///     in the given part.
    /// </summary>
    /// <param name="uid">The part entity id to check on.</param>
    /// <param name="type">The type of component to check for.</param>
    /// <param name="part">The part to check for organs on.</param>
    /// <param name="organs">The organs found on the body part.</param>
    /// <returns>Whether any were found.</returns>
    /// <remarks>
    ///     This method is somewhat of a copout to the fact that we can't use reflection to generically
    ///     get the type of component on runtime due to sandboxing. So we simply do a HasComp check for each organ.
    /// </remarks>
    public bool TryGetBodyPartOrgans(
        EntityUid uid,
        Type type,
        [NotNullWhen(true)] out List<(EntityUid Id, OrganComponent Organ)>? organs,
        BodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
        {
            organs = null;
            return false;
        }

        var list = new List<(EntityUid Id, OrganComponent Organ)>();

        foreach (var organ in GetPartOrgans(uid, part))
        {
            if (HasComp(organ.Id, type))
                list.Add((organ.Id, organ.Component));
        }

        if (list.Count != 0)
        {
            organs = list;
            return true;
        }

        organs = null;
        return false;
    }

    private bool TryGetPartSlotContainerName(BodyPartType partType, out HashSet<string> containerNames)
    {
        containerNames = partType switch
        {
            BodyPartType.Hand => new() { "gloves" },
            BodyPartType.Foot => new() { "shoes" },
            BodyPartType.Head => new() { "eyes", "ears", "head", "mask" },
            _ => new()
        };
        return containerNames.Count > 0;
    }

    private bool TryGetPartFromSlotContainer(string slot, out BodyPartType? partType)
    {
        partType = slot switch
        {
            "gloves" => BodyPartType.Hand,
            "shoes" => BodyPartType.Foot,
            "eyes" or "ears" or "head" or "mask" => BodyPartType.Head,
            _ => null
        };
        return partType is not null;
    }

    public int GetBodyPartCount(EntityUid bodyId, BodyPartType partType, BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, logMissing: false))
            return 0;

        int count = 0;
        foreach (var part in GetBodyChildren(bodyId, body))
        {
            if (part.Component.PartType == partType)
                count++;
        }
        return count;
    }

    public string GetSlotFromBodyPart(BodyPartComponent? part)
    {
        var slotName = "";

        if (part is null)
            return slotName;

        if (part.SlotId != "")
            slotName = part.SlotId;
        else
            slotName = part.PartType.ToString().ToLower();

        if (part.Symmetry != BodyPartSymmetry.None)
            return $"{part.Symmetry.ToString().ToLower()} {slotName}";
        else
            return slotName;
    }

    // Shitmed Change End

    /// <summary>
    /// Gets the parent body part and all immediate child body parts for the partId.
    /// </summary>
    public IEnumerable<EntityUid> GetBodyPartAdjacentParts(
        EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        if (TryGetParentBodyPart(partId, out var parentUid, out _))
            yield return parentUid.Value;

        foreach (var slotId in part.Children.Keys)
        {
            var container = Containers.GetContainer(partId, GetPartSlotContainerId(slotId));

            foreach (var containedEnt in container.ContainedEntities)
            {
                yield return containedEnt;
            }
        }
    }

    public IEnumerable<(EntityUid AdjacentId, T Component)> GetBodyPartAdjacentPartsComponents<T>(
        EntityUid partId,
        BodyPartComponent? part = null)
        where T : IComponent
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        var query = GetEntityQuery<T>();
        foreach (var adjacentId in GetBodyPartAdjacentParts(partId, part))
        {
            if (query.TryGetComponent(adjacentId, out var component))
                yield return (adjacentId, component);
        }
    }

    public bool TryGetBodyPartAdjacentPartsComponents<T>(
        EntityUid partId,
        [NotNullWhen(true)] out List<(EntityUid AdjacentId, T Component)>? comps,
        BodyPartComponent? part = null)
        where T : IComponent
    {
        if (!Resolve(partId, ref part, logMissing: false))
        {
            comps = null;
            return false;
        }

        var query = GetEntityQuery<T>();
        comps = new List<(EntityUid AdjacentId, T Component)>();

        foreach (var adjacentId in GetBodyPartAdjacentParts(partId, part))
        {
            if (query.TryGetComponent(adjacentId, out var component))
                comps.Add((adjacentId, component));
        }

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }

    #endregion
}
