// SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 ZeroDayDaemon <60460608+ZeroDayDaemon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 QueerCats <jansencheng3@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Inventory;

/// <summary>
///     Defines what slot types an item can fit into.
/// </summary>
[Serializable, NetSerializable]
[Flags]
public enum SlotFlags
{
    NONE = 0,
    PREVENTEQUIP = 1 << 0,
    HEAD = 1 << 1,
    EYES = 1 << 2,
    EARS = 1 << 3,
    MASK = 1 << 4,
    OUTERCLOTHING = 1 << 5,
    INNERCLOTHING = 1 << 6,
    NECK = 1 << 7,
    BACK = 1 << 8,
    BELT = 1 << 9,
    GLOVES = 1 << 10,
    IDCARD = 1 << 11,
    POCKET = 1 << 12,
    LEGS = 1 << 13,
    FEET = 1 << 14,
    BACKSTORAGE = 1 << 15,
    TANKSTORAGE = 1 << 16,
    All = ~NONE,

    WITHOUT_POCKET = All & ~POCKET
}
