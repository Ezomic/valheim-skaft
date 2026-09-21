# Changelog

## 1.3.0 - 22 September 2026

One line under the crosshair. No change to what a swing or a press does.

### Added

- **`Damaged in reach: 13` under the crosshair**, while a repair entry is selected and you are
  pointing at a piece. It counts the piece under the cursor and every damaged piece within your
  current reach of it.

  When the piece under the cursor is intact the line reads `Damaged in reach: 12 (aim at a
  damaged piece)`, because that is the one swing the mod will not answer. When nothing in reach
  is damaged it reads `Damaged in reach: none`. Below the reach curve there is no line at all,
  the same way there is no sweep.

  `ShowDamagedInReach` turns it off. Like the build menu line it is display rather than balance,
  so a host running Core does not get to decide it for anybody.

### Why a count is worth a line of its own

**Damage above three quarters is invisible.** `WearNTear.UpdateVisual` swaps in the worn model
below 0.75 health and the broken one below 0.25, and does nothing above that - so a wall at 90%
looks exactly like a wall at 100%, and a base that has been rained on for a week looks fine. The
health bar tells you about the one piece you are already pointing at, which is the piece you
least need telling about.

**And a swing at an intact piece does nothing.** The sweep only follows a repair vanilla itself
just made, which is what buys it build mode, the station requirement, ward access and the repair
cooldown for free. The cost of that is a rule nothing in the game states: point at something
broken. It has been the first entry in this mod's troubleshooting section since 1.0.0, which is
the wrong place to keep an answer.

### What the number is, exactly

Damaged, and in reach. It is the same pass the swing itself makes to find candidates, called
from the HUD instead of from the hammer, so the two cannot drift apart. Everything that can
refuse a piece afterwards still refuses it: stamina, hammer durability, `MaxPieces`, wards and a
missing crafting station. So the count is what the swing will attempt, not a promise about what
it will finish, and on a long wall the stamina bar will usually stop first.

It is recounted whenever the cursor moves to a different piece, and four times a second while it
holds still.

### What has actually been run

Nothing, in game. This builds and is reasoned from the decompiled `Hud` and `WearNTear`. The
0.75 and 0.25 thresholds are read off `UpdateVisual`.

## 1.2.0 - 22 September 2026

The bench half of repair. The hammer sweep is untouched.

### Added

- **One press of the Repair button at a crafting station now repairs several worn items**, and
  how many is your Crafting skill. One at level 0, two from about 14, three around 25, seven
  around 50 and ten from 60 - ten being a full kit, which is helmet, chest, legs, cape, weapon,
  shield, bow and the three tools. So coming back from the swamp is one press at the top of the
  curve and ten presses at the bottom of it, which is where the game already had you.

  The count is `MinItems + (MaxItems - MinItems) * (level / FullLevel)^ItemCurve`, floored, read
  fresh on every press. `FullLevel` is the same entry the radius uses: one skill, one level at
  which it has arrived.

### Why a count, when the sweep charges a price

Because there is no price here to charge. Vanilla asks for no materials, no durability and no
stamina to repair an item at a bench - the presses were the whole cost of it, which is why
repairing a kit is tedious rather than expensive. A mod that cut a price here would be cutting
nothing. So the constraint is the number itself: it starts at vanilla's one, and the skill is
what buys it up.

`ItemCurve` is its own entry rather than the radius's `Curve`, and it sits above 1 where that one
sits below it. A count is a much coarser dial than metres: one more metre of reach is nothing,
one more item is the difference between pressing twice and pressing once. Sharing the radius
curve would have handed out three items a press at Crafting 10, at a level where the radius is
deliberately still worth nothing.

### The skill it pays is vanilla's, unchanged

`RepairOneItem` raises Crafting by how worn the item was, once per item. This repairs items the
same way and grants the same amount for each, so nine items in one press raise Crafting by
exactly what nine presses raised it by. That matters because Crafting is what buys the count: if
the payout had been per press rather than per item, the mod would have been feeding the skill
that grants it. It is not. Only the pressing changes.

### What it leaves alone

It does not decide which items may be repaired. `InventoryGui.CanRepair` does, as it always has,
which carries the recipe lookup, the repair-station match, the station level and the world level.
A bench too low for your armour still refuses it, and refuses it here for the same reason.

It does not fire the station effect once per item, and does not report one item at a time. One
press sounds like one press and says `Repaired 9 items` in the middle of the screen, on vanilla's
own `$msg_repaired` line with a count where the item name goes. Naming one of nine items and
putting `x9` after it - the trick the sweep uses for a wall repaired thirteen times - would be a
lie about which nine things were fixed.

### Changed

- `Enabled` now means the whole mod rather than only the sweep. `RepairItems` is the switch that
  keeps one half and drops the other.
- `FullLevel` now drives both halves.

### What has actually been run

All of it, on 22 September 2026, in a live singleplayer world, driven start to finish by a Devkit
scenario so that it can be run again: twelve worn crude bows at a roofed workbench, and one press
of the Repair button fixed ten at Crafting 60, one at Crafting 0 and three at Crafting 25.

Those three numbers are the point of running it. Any one of them alone is satisfied by several
wrong implementations - ten is also what "repair everything" does, one is also what a mod that
never ran does - and only the curve produces all three from the same press.

The skill payout is vanilla's, and it showed itself without being asked: the single repair at
Crafting 0 had taken Crafting to 1 by the next step, which is `RepairOneItem` granting exactly
what it always granted, with the mod adding nothing of its own.

Not exercised: a dedicated server, a second player, and a station too low for the item being
repaired. That last one is vanilla's `CanRepair` either way, which is why it was left to it.

## 1.1.1 - 12 September 2026

### Changed

- Rewritten README. Same mod, clearer documentation: what it does and how to install it come
  first, then configuration, multiplayer behaviour, compatibility and troubleshooting. Every
  config table was checked against the plugin's own Config.Bind calls, so the settings,
  sections and defaults listed are the ones actually bound. No code changed in this release.

## 1.1.0 - 9 September 2026

Rebuilt for Valheim 1.0. This version does not run on pre-1.0 Valheim, and the previous
one does not run on 1.0.

### Fixed

- **Reads the right global key on Valheim 1.0.** `GlobalKeys` is the one implicitly numbered
  enum in the game's API, and 1.0 inserted ten members - moving the no-workbench key from 22 to
  27. A compiled ordinal therefore asked about a different key entirely, with nothing logged.
  Read by name now, taken off the enum member so a rename follows automatically and a removal
  is a build error rather than a quiet wrong answer.

### Changed

- **Joins the Longhouse pack.** Nothing here changed to allow that; Core was discarding the
  requirement each mod declares, so a server without Skaft refused every client that had it.

## 1.0.0 - 2 September 2026

First version. Repairing with the hammer sweeps everything damaged within reach of the piece
you hit, and the reach is your Crafting skill.

### The reach

Nothing at Crafting 0, about two metres at 10, four at 25, seven at 50, and eight from 60 up.
The curve is `min + (max-min) * (level/FullLevel)^0.8` and all four numbers are config entries.

It stops growing at 60 rather than 100 on purpose. Crafting costs roughly 20,300 crafts to
reach 100 and about 5,700 to reach 60, so a curve normalised to the top of the bar puts its
payoff somewhere no character arrives. The exponent sits below 1 for the same reason: the
experience curve is already steep at the top, and a second brake stacked on it hides the whole
mod behind a wall.

### The price

Each piece the sweep repairs is charged the stamina, eitr and hammer durability that repairing
it by hand would have cost. That is the difference between this and every other area repair:
the radius says how far you may reach, and the stamina bar says how much of it you can afford.

Vanilla's own price does fall as Crafting rises, and that is kept. The Hammer's piece table
names Crafting, so building and repairing already cost up to half as much stamina at high
skill: measured in game, 5.00 a piece at Crafting 0, 3.50 at 60, 2.50 at 100. Cancelling that
would mean writing a rule against a rule the game already has. It also means the levels above
60 are not wasted once the radius stops growing, because they keep buying pieces per stamina
bar instead of metres.

### What it will not do

It will not train Crafting, because repairing a building never has, and a reward that feeds
the skill granting it is a loop rather than a design.

It will not break your hammer inside a sweep. Repairing subtracts durability without checking
zero, so a wide sweep can spend a whole hammer on one press. The game does tell you it broke,
but it also unequips it, and being dropped out of build mode by a single click is a different
thing from wearing a tool down over the swings that did it.

It will not fire the build effect, the swing animation or a message per piece. One swing
already fired all three for the piece under the cursor; forty more of each in the same frame is
forty particle bursts, forty broadcast animation calls and forty seconds of corner messages,
since those drain at one per second.

### Point at something broken

The sweep runs only when the piece under your cursor was itself repaired by the swing. That is
deliberate. It is how the mod inherits build mode, the crafting station requirement, ward
access and the game's own one second per piece repair cooldown, rather than keeping copies of
all four and maintaining them through updates.

The visible consequence is that hovering an intact wall beside a damaged one does nothing, and
a second click within a second does nothing.

### What has actually been run

The sweep works. At Crafting 55 the reach measured 7.5m, which is what the curve predicts, and
one swing at a damaged wall repaired the two damaged walls beside it and left the intact ones
alone. The radius, the trigger, the health filter and the charge are all confirmed in a live
singleplayer world, on 28 August 2026.

The two pieces of feedback are confirmed too, which matters because neither could be proved by
reading code alone. The corner message reads `Repaired Wood wall x3` - one line rather than
three, because the game sums the amounts of two matching messages queued within four seconds,
so the count rides vanilla's own text and needs no new translation. And the Repair entry in the
build menu carries its reach line.

Not yet exercised: wards, another player's pieces, running out of stamina or durability
mid-sweep, and anything at all in multiplayer. Singleplayer is not a weaker version of a
multiplayer test, it is a different one - there is no owner to hand a repair to and no gate to
pass - so nothing above should be read as covering a server.

### What a Core server does to this

Skaft registers with Core's gate as `HostOnly`, and that word describes one direction only. A
client *without* Skaft joining a server with it is let through, which is the whole reason the
requirement is set that way: such a client is genuinely unaffected, it simply repairs one piece
a swing.

Going the other way, the gate refuses. The manifest each end sends does include the
requirement, but the end reading it discards that field on purpose - its own view is what it
enforces - so the check for mods present on the far end and absent here has nothing left to
tell it that this one is allowed to be one-sided, and reports it as a mismatch. A server
running Core without Skaft therefore turns away every client that has it, showing the game's
own incompatible-version screen. On a Core server it is installed on the server too, or on
nobody. A server with no Core has no gate and does not care.

That is a Core matter rather than a Skaft one, and it is cheap to change if it should be: the
field is already on the wire and only the comparison would move.

### These defaults are the ones people keep

BepInEx writes every bound entry to disk the first time the plugin loads, and from then on the
saved value beats any new default in code. So the curve, the radius pair and `CostMultiplier`
shipped here are permanent for anybody who installs this version: retuning them later moves
new installs and nobody else.

That is worth saying out loud in the first published version rather than discovering it in the
second, because the numbers have been measured at exactly one point - 7.5m at Crafting 55 -
and reasoned everywhere else.
