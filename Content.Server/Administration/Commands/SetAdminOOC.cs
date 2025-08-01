// SPDX-FileCopyrightText: 2021 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Galactic Chimp <63882831+GalacticChimp@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Leo <lzimann@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Brandon Hu <103440971+Brandon-Huu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.NameColor)]
    internal sealed class SetAdminOOC : IConsoleCommand
    {
        public string Command => "setadminooc";
        public string Description => Loc.GetString("set-admin-ooc-command-description", ("command", Command));
        public string Help => Loc.GetString("set-admin-ooc-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (args.Length < 1)
                return;

            var colorArg = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(colorArg))
                return;

            var color = Color.TryFromHex(colorArg);
            if (!color.HasValue)
            {
                shell.WriteError(Loc.GetString("shell-invalid-color-hex"));
                return;
            }

            var userId = shell.Player.UserId;
            // Save the DB
            var dbMan = IoCManager.Resolve<IServerDbManager>();
            dbMan.SaveAdminOOCColorAsync(userId, color.Value);
            // Update the cached preference
            var prefManager = IoCManager.Resolve<IServerPreferencesManager>();
            var prefs = prefManager.GetPreferences(userId);
            prefs.AdminOOCColor = color.Value;
        }
    }
}
