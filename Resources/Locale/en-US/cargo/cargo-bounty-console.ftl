# SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 Killerqu00 <47712032+Killerqu00@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 lzk <124214523+lzk228@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Gansu <68031780+GansuLalan@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Tay <td12233a@gmail.com>
# SPDX-FileCopyrightText: 2025 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
#
# SPDX-License-Identifier: MIT

bounty-console-menu-title = Cargo bounty console
bounty-console-label-button-text = Print label
bounty-console-skip-button-text = Skip
bounty-console-time-label = Time: [color=orange]{$time}[/color]
bounty-console-reward-label = Reward: [color=limegreen]${$reward}[/color]
bounty-console-manifest-label = Manifest: [color=orange]{$item}[/color]
bounty-console-manifest-entry =
    { $amount ->
        [1] {$item}
        *[other] {$item} x{$amount}
    }
bounty-console-manifest-entry-reagent =
    { $amount ->
    [1] {$item}
    *[other] {$item} {$amount}u
    }
bounty-console-manifest-reward = Reward: ${$reward}
bounty-console-description-label = [color=gray]{$description}[/color]
bounty-console-id-label = ID#{$id}

bounty-console-flavor-left = Bounties sourced from local unscrupulous dealers.
bounty-console-flavor-right = v1.4

bounty-manifest-header = [font size=14][bold]Official cargo bounty manifest[/bold] (ID#{$id})[/font]
bounty-manifest-list-start = Item manifest:

bounty-console-tab-available-label = Available
bounty-console-tab-history-label = History
bounty-console-history-empty-label = No bounty history found
bounty-console-history-notice-completed-label = [color=limegreen]Completed[/color]
bounty-console-history-notice-skipped-label = [color=red]Skipped[/color] by {$id}

bounty-console-category-description = {$category} Bounty: {$id}
