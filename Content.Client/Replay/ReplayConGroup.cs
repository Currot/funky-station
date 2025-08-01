// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Client.Console;

namespace Content.Client.Replay;

public sealed class ReplayConGroup : IClientConGroupImplementation
{
    public event Action? ConGroupUpdated { add { } remove { } }
    public bool CanAdminMenu() => true;
    public bool CanAdminPlace() => true;
    public bool CanCommand(string cmdName) => true;
    public bool CanScript() => true;
    public bool CanViewVar() => true;
}
