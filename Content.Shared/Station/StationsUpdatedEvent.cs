// SPDX-FileCopyrightText: 2022 Moony <moonheart08@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Station;

[NetSerializable, Serializable]
public sealed class StationsUpdatedEvent : EntityEventArgs
{
    public readonly List<(string Name, NetEntity Entity)> Stations;

    public StationsUpdatedEvent(List<(string Name, NetEntity Entity)> stations)
    {
        Stations = stations;
    }
}
