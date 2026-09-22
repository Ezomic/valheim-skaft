# Skaft

Skaft is about repair, and the Crafting skill is what it pays you in.

With the hammer it turns repair into an area repair, and the size of that area is your Crafting
skill. Every piece the sweep fixes costs the same stamina, eitr and hammer durability that
repairing it by hand would have cost, so the skill decides how far you reach and your stamina
bar decides how much of that you can afford in one swing.

At a crafting station the same skill decides how many worn items one press of the Repair button
puts right. Vanilla charges nothing to repair an item, so there is no price there to cut and the
count is the whole of it: one item a press at Crafting 0, a full kit from 60.

*Skaft* is Old Norse for the shaft of a tool.

## Features

- Repairing a damaged piece with the hammer also repairs the damaged pieces around it.
- The radius comes from Crafting: nothing at level 0, about 2m at 10, 4m at 25, 7m at 50, and
  8m from 60 upwards.
- Each swept piece is charged the full vanilla per-piece repair cost. Nothing is free.
- Pieces are repaired nearest first, so a swing that runs out of stamina has fixed the wall in
  front of you.
- The Repair entry in the build menu shows your current reach in metres and the Crafting level
  it came from, and the crosshair shows how many damaged pieces are in reach of what you are
  pointing at.
- The result arrives in the usual corner message as `Repaired Wood wall x13`, using the game's
  own text.
- The sweep will not break your hammer inside one click; it stops a durability point short.
- One press of the Repair button at a bench repairs several worn items: one at Crafting 0, three
  around 25, seven around 50 and ten from 60.
- The bench half grants vanilla's own skill, per item, so nine items in one press raise Crafting
  by exactly what nine presses raised it by.
- Either half can be switched off on its own.
- No new prefabs, items, recipes or saved values. A world played with Skaft is an ordinary
  world, and removing the mod leaves nothing behind.

## How the sweep works

Take out the hammer, select Repair, and hit something damaged. Everything damaged within reach
of that piece is repaired until your stamina or your hammer durability runs out.

**Point at something broken.** With a repair entry selected, the crosshair says how many damaged
pieces are in reach of the one you are aiming at - and says `aim at a damaged piece` when that
one is intact, because the sweep only runs when the piece under your cursor was itself repaired
by that swing.

That line is worth having because damage is mostly invisible. The game swaps in the worn model
below three quarters health and the broken one below a quarter, and shows nothing at all above
that, so a wall at 90% looks new. The count is damage and distance only: stamina, hammer
durability, wards and a missing station can still cut the swing short, so it is what the swing
will attempt rather than a promise about what it will finish.

A second click within a second also does nothing, because vanilla holds each piece on a one
second repair cooldown. Following vanilla's own success is how the mod inherits every check the
game already makes: build mode, the crafting station a piece requires, ward access, and the
full-health test.

The radius is `MinRadius + (MaxRadius - MinRadius) * (level / FullLevel)^Curve`, read fresh on
every swing. With the default numbers:

| Crafting | Reach |
| --- | --- |
| 0 | none |
| 10 | 1.9m |
| 25 | 4.0m |
| 50 | 6.9m |
| 60 and above | 8.0m |

FullLevel is 60 rather than 100 because Crafting 100 costs roughly 20,300 crafts and level 60
about 5,700, and repairing buildings trains no skill at all. Crafting rises from crafting and
upgrading at a station and from repairing worn items.

Vanilla already makes building and repairing cheaper as Crafting rises: the Hammer's piece
table names the Crafting skill, so `GetBuildStamina` subtracts up to half the cost at max
level. Measured in game that is 5.00 stamina a piece at Crafting 0, 3.50 at 60 and 2.50 at
100. Skaft does not cancel that, which means the levels above 60 still pay you: the radius
stops growing but each swing affords more pieces.

### What it does not do

- It does not train Crafting for a building repair. Repairing a building has never given skill
  in this game, and the sweep does not invent it. Repairing an *item* at a bench always has, and
  that payout is left exactly as vanilla has it: per item, sized by how worn the item was.
- It does not repair anything the hammer could not repair by hand. Other players' buildings
  yes, exactly as vanilla does, with wards as the permission system in both cases. Objects
  that are not build pieces, such as dvergr props and Ashlands altars, are skipped.
- It does not fire the build effect, the swing animation or a corner message per piece. One
  swing already fired those once for the piece under the cursor.

## The bench

Stand at a crafting station, open it, and press Repair. Vanilla repairs one worn item per press.
Skaft repairs as many as your Crafting allows:

| Crafting | Items per press |
| --- | --- |
| 0 | 1 |
| 14 | 2 |
| 25 | 3 |
| 50 | 7 |
| 60 and above | 10 |

The count is `MinItems + (MaxItems - MinItems) * (level / FullLevel)^ItemCurve`, floored, and
`FullLevel` is the same entry the radius uses. Ten at the top is a full kit: helmet, chest, legs,
cape, weapon, shield, bow and the three tools.

**There is no price to charge here, so the count is the constraint.** Repairing an item at a
bench costs no materials, no durability and no stamina in vanilla, which is why a kit is tedious
to repair rather than expensive. The only cost was the pressing, so that is what the skill buys
down, starting from vanilla's one.

`ItemCurve` is a separate entry from the radius's `Curve`, and it sits above 1 where that one
sits below it. One more metre of reach is nothing; one more item is the difference between
pressing twice and pressing once, so the count is handed over more slowly than the metres are.

### What the bench half does not change

- It does not decide which items may be repaired. The game's own check does, with the recipe,
  the repair station, the station level and the world level. A bench too low for your armour
  still refuses it.
- It does not change the skill. Crafting is granted per item, by the amount vanilla grants, so
  the mod never pays you for pressing less.
- It does not repair items in a chest, on the ground or on anybody else. It is your inventory,
  which is what the button has always meant.
- It does not fire the station effect or a message per item. One press sounds like one press and
  says `Repaired 9 items` in the middle of the screen.

## Installation

Install through a mod manager from
[Thunderstore](https://thunderstore.io/c/valheim/p/Ezomic/Skaft/), or by hand: put `Skaft.dll`
in `BepInEx/plugins/Skaft/`.

Requires [BepInEx 5.4.2350](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
This is a BepInEx 5 plugin and does not work on BepInEx 6.

[Longhouse Core](https://thunderstore.io/c/valheim/p/Ezomic/Longhouse_Core/) is an optional
soft dependency. Skaft runs without it; see Multiplayer for what Core adds.

The config file does not exist until the game has been started once with the mod installed.

## Configuration

The file is `BepInEx/config/ezomic.valheim.skaft.cfg`. Every entry has a comment above it
explaining the number.

### [Skaft]

| Key | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | The whole mod, both halves. Off leaves vanilla single-piece repair untouched in both places. |
| `MinRadius` | `0` | Reach in metres at Crafting 0. At zero the sweep does not run at all, so a new character gets plain vanilla repair. |
| `MaxRadius` | `8` | Reach in metres once Crafting reaches `FullLevel`. At 8, a mid-wall swing covers a 10x6 longhouse end to end. |
| `FullLevel` | `60` | The Crafting level at which the radius stops growing and a bench press reaches `MaxItems`. Shared by both halves. |
| `Curve` | `0.8` | Exponent on the skill fraction. 1.0 is a straight line, below 1 opens the reach earlier, above 1 saves it for the top levels. |
| `CostMultiplier` | `1` | Multiplies the stamina, eitr and durability charged per swept piece. 1 is exactly what vanilla charges to repair that piece by hand. Pieces already at full health cost nothing either way. |
| `DurabilityFloor` | `1` | The sweep stops before the hammer would drop to or below this. At 0 a sweep can spend the hammer to its last point, which unequips it and drops you out of build mode mid-job. |
| `MaxPieces` | `200` | Hard ceiling on pieces repaired in one swing. This is a network guard, not a balance number: each repaired piece is one message out and one broadcast back. |
| `RepairItems` | `true` | Whether one press of the Repair button at a bench repairs more than one item. Off leaves the bench exactly as vanilla has it; the hammer sweep is unaffected either way. |
| `MinItems` | `1` | Items one press repairs at Crafting 0. One is vanilla. |
| `MaxItems` | `10` | Items one press repairs once Crafting reaches `FullLevel`. Ten is a full kit. |
| `ItemCurve` | `1.5` | Exponent on the skill fraction for the bench. Above 1 saves the count for the higher levels, which is the opposite of what `Curve` does for the radius, and deliberately so. |
| `OwnBuildingsOnly` | `false` | Restrict the sweep to pieces you placed. False matches vanilla, which repairs anyone's building and leaves permission to wards. The piece directly under your cursor is always vanilla's business, not this setting's. |
| `ShowReachInBuildMenu` | `true` | Add the reach line to the Repair entry in the build menu. |
| `ShowDamagedInReach` | `true` | Show `Damaged in reach: 13` under the crosshair while a repair entry is selected and you are pointing at a piece. Counts damage and distance only. |
| `ReachEntries` | `piece_repair` | Comma separated prefab names of the build menu entries the reach line is written on. Other mods hang their own click-on-the-world tools off the same flag, and the sweep can never run on those, so the line is only written on names listed here. |

### [Diagnostics]

| Key | Default | Effect |
| --- | --- | --- |
| `Verbose` | `false` | One line per swing to `BepInEx/LogOutput.log`: skill level, radius, candidates in range, pieces repaired, and what stopped the sweep. Also names any repair entry that is not in `ReachEntries`, and logs the hammer's real numbers once per session. |

BepInEx writes every setting to disk the first time the mod loads, and from then on the saved
value beats a new default in code. If a later version retunes the curve, machines that already
have this one keep the old numbers unless the cfg line is edited or deleted.

## Multiplayer

Skaft works for whoever installs it. Every decision is made on the client swinging the hammer or
pressing the button, from state that client already has, and the only thing that leaves the
machine is the repair message vanilla would have sent anyway. Players without the mod are
unaffected: they repair one piece per swing and one item per press.

The bench half never touches the network at all. It writes durability on items in your own
inventory, which is the field vanilla's own Repair button sets, and nobody else can see it.

None of this can be enforced by the server. `WearNTear.RPC_Repair` has no permission check and
the server forwards it without inspecting the sender, so any client has always been able to
repair any loaded piece. Checking a radius server-side would mean a second protocol for
something that was never constrained in the first place.

With Longhouse Core installed on both ends, the host's settings are applied on connected
clients in memory for as long as they are connected. The client's own config file is not
written, and their values come back on disconnect. That is what makes the curve a property of
the server rather than an agreement between players: without it, anyone can set `MaxRadius` to
100 and `CostMultiplier` to 0 in their own file. `RepairItems`, `MinItems`, `MaxItems` and
`ItemCurve` are synced for the same reason. `ShowReachInBuildMenu`, `ShowDamagedInReach` and
`Verbose` are exempt, since they are display and diagnostics rather than balance.

Skaft registers with Core as host-only, so a client without Skaft can join a server that has
it. From Core 1.2.0 that also works the other way: a client with Skaft can join a Core server
that does not have it. On older Core versions the server refused those clients with the game's
stock incompatible-version screen, which is why Skaft 1.0.0 shipped outside the Longhouse pack.

## Compatibility

Built against Valheim 1.0.7, BepInEx 5.4.23.5 and Harmony 2.9. Version 1.1.0 and later do not
run on pre-1.0 Valheim, and 1.0.0 does not run on 1.0.

Skaft adds four Harmony postfixes, on `Player.Repair`, `Player.UpdatePlacement`,
`InventoryGui.RepairOneItem` and `Hud.UpdateCrosshair`, and patches nothing else. Another mod that prefixes `Player.Repair`
and skips the original makes the sweep inert for that tool, which is correct: Vaettir's
Transplant entry on the cultivator does exactly that, and no sweep should happen there.

Do not run a second area repair mod alongside this one. Both would act on the same swing. The
same goes for a second repair-all-items mod: both would act on the same press, and the second
one would find nothing left to do or repair past this one's count.

## Troubleshooting

**Nothing happens when I swing.** The sweep only follows a repair that vanilla itself just
made, so aim at a damaged piece rather than at an intact one next to it. The crosshair says
which of those you are looking at. Below Crafting 10 the reach is under 2m, which will not
reach a neighbouring piece.

**Only a few pieces get repaired.** That is the stamina bar, not the radius. The sweep also
stops at `DurabilityFloor` and at `MaxPieces`. Turn on `Verbose` and the log line says which of
the four stopped it.

**No line under the crosshair.** Four things have to be true and any one of them missing gives
you nothing: the hammer is out with **Repair** selected rather than a build piece, the crosshair
is actually on a piece (you will see its health bar), your Crafting is **above 0**, and
`ShowDamagedInReach` is on. The third is the one that catches people. At Crafting 0 there is no
reach, so there is no line - deliberately, because a character who has not earned it is not told
about it - and that is indistinguishable from the mod being broken unless you know.

**No reach line in the build menu.** Either `ShowReachInBuildMenu` is off, or the repair entry
you selected is not named in `ReachEntries`. With `Verbose` on, the log names the entry that
was actually selected, once per name.

**A setting had no effect.** Check the cfg file first: BepInEx keeps the saved value. On a
server running Core, the host's value is applied over yours while you are connected.

**The log says it could not reach `Player.GetBuildStamina` or `WearNTear.m_lastRepair`.** A
game update moved those members. The sweep switches off and repair stays vanilla until the mod
is rebuilt.

## Status

The sweep has been played in single player and nowhere else. In a live world on 28 August 2026,
at Crafting 55, the reach measured 7.5m, which is what the curve predicts, and one swing at a
damaged wall repaired the two damaged walls beside it and left the intact ones alone. The
radius, the trigger rule, the health filter, the per-piece charge, the corner message and the
build menu line are confirmed there.

**The bench half has been run, and the curve is the curve.** On 22 September 2026 a Devkit
scenario drove it in a live singleplayer world: twelve worn crude bows at a roofed workbench, and
one press of Repair fixed **ten** of them at Crafting 60, **one** at Crafting 0 and **three** at
Crafting 25. Those three numbers together are the shape of the curve, and no other shape produces
all three.

The skill payout is vanilla's, and it showed itself without being asked: the single repair at
Crafting 0 had taken Crafting to 1 by the next step of the scenario, which is `RepairOneItem`
granting what it always granted and the mod adding nothing of its own.

**The crosshair count has been run too.** A scenario drove five walls through the four states
the line has - nothing damaged, a whole piece with broken neighbours, a broken piece, no reach -
aiming the camera and letting the game's own raycast find the piece rather than writing the
hover by hand. Confirmed by eye at Crafting 50 on the same day.

Not yet exercised: a dedicated server, a second player, another player's buildings, wards, and
running out of stamina or hammer durability part-way through a swing. The ward argument is that
the sweep inherits vanilla's checks because it only runs on a piece vanilla just repaired, which
is a reason to expect it to be right rather than a report of it being right.

## Bug reports

Report in the [Discord](https://discord.gg/hJzAVaZ5wb) or on the
[issue tracker](https://github.com/Ezomic/valheim-skaft/issues). Useful to attach:

- `BepInEx\LogOutput.log`, ideally with `Verbose` set to `true` in the config.
- Whether you were in single player, hosting, or on a dedicated server.
- `BepInEx\config\ezomic.valheim.skaft.cfg`.
- `AppData\LocalLow\IronGate\Valheim\Player.log` if a vanilla mechanic broke. Exceptions thrown
  mid-frame land there rather than in the BepInEx log.

## Bugs and ideas

Both go to the site. [longhouse.thijssensoftware.nl/bugs](https://longhouse.thijssensoftware.nl/bugs)
is for anything broken, and [longhouse.thijssensoftware.nl/ideas](https://longhouse.thijssensoftware.nl/ideas)
is for what a mod should do next. You can vote on other people's ideas there as well.

Signing in takes a Steam or Discord account. I work from that list, so the votes decide what
I pick up next.

## Discord

The [Discord](https://discord.gg/hJzAVaZ5wb) is where mod information, updates, support, bug
reports and compatibility questions go.

## Server

There is a small EU server running the Longhouse pack if you want somewhere to play.
Connection details are in the Discord.

## Licence

MIT. See `LICENSE`.

## Part of Longhouse

Skaft is included in the [Longhouse](https://thunderstore.io/c/valheim/p/Ezomic/Longhouse/)
modpack, which pins the exact versions its members run. It behaves identically installed on its
own.
