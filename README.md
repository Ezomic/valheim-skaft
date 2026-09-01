# Skaft

Repairing with the hammer also repairs what is around it, and how far that reaches is your
Crafting skill.

*Skaft* is the Old Norse word for the shaft of a tool, the part you hold. The shaft is what
decides how far the head reaches, and it is the part that wears out in your hand.

## Why

Area repair is one of the most installed things in Valheim and the reason is fair: walking a
longhouse wall by wall after a troll has been through it is tedious rather than interesting.
The problem with the versions on offer is not the radius. It is that the radius is a config
entry. You install the mod, you type a number, and maintenance is over for the rest of the
save.

This one hands out the same convenience and makes it something the character earned. A fresh
character gets vanilla repair, one piece per swing, and cannot tell the mod is installed. The
reach opens as Crafting rises, and it is worth having by the time the base is worth defending.

It also never makes repairing free. Every piece the sweep touches is charged exactly the
stamina, eitr and hammer wear that repairing it by hand would have cost. So there are two
limits doing two different jobs: the skill decides how far you can reach, and your stamina bar
decides how much of that you can afford in one swing. A wide reach on an empty bar fixes
nothing.

The obvious alternative was to scale the *cost* down as the skill rises instead. It was
rejected for the same reason as the config number: it ends at maintenance being free, and a
mod that deletes a system is a different mod from one that makes it quicker.

## Using it

Take out the hammer, pick Repair, and hit something broken. Everything within reach that is
also damaged gets repaired, nearest first, until your stamina or your hammer runs out.

The count appears in the usual corner message, as `Repaired Wood wall x13`. That number is the
only readout worth having, because it answers the question you actually asked.

Your current reach is written on the Repair entry in the build menu, in metres, beside the
Crafting level it came from.

**Point at something broken.** The sweep only runs when the piece under your cursor was itself
repaired by the swing. Hovering an intact wall next to a damaged one does nothing, and a second
click within a second does nothing, because the game holds each piece on a one second repair
cooldown of its own.

That rule is not a limitation that got left in. It is what lets the mod inherit every check the
game already makes on a repair: build mode, the crafting station a piece needs, wards, and
whatever a future update adds. The alternative was to copy those checks into the mod and
maintain the copies.

## What it does not do

It does not train Crafting. Repairing a building has never given skill in this game, and adding
it would mean the reward feeding the skill that grants it. Crafting is earned at the bench and
spent at the wall.

It does not repair anything the hammer could not repair by hand. Other people's buildings, yes,
exactly as vanilla does, and wards are the permission system in both cases. Things that are not
build pieces, no.

It does not break your hammer. Repairing subtracts durability without checking zero, so a wide
sweep could spend a whole hammer on one press and unequip it mid job. The sweep stops a point
short instead, and ordinary swinging still wears the hammer out the normal way.

## Installing

Needs BepInEx and nothing else. By hand, put `Skaft.dll` in `BepInEx/plugins/Skaft/`.

Start the game once and quit if you want the config file to edit. It does not exist until the
mod has loaded once, which is the usual reason people think a setting is missing.

## Settings

The file is `BepInEx/config/ezomic.valheim.skaft.cfg`. Every setting has a comment above it,
so the file explains itself. The two worth knowing about are `FullLevel`, the Crafting level
where the reach stops growing, and `CostMultiplier`, which is the price per piece and is set to
match vanilla exactly.

Changing a default in a new version does nothing on a machine that has already run the mod.
BepInEx writes every entry on first run and the saved value wins.

## Multiplayer

**The host needs it.** Clients without it are let in and are unaffected, because they simply
repair one piece at a swing. Nothing this mod does leaves the machine except the repair message
the game would have sent anyway, and there is no prefab, no item change and no saved value of
its own, so a world built with Skaft is an ordinary world.

If [Core](https://github.com/Ezomic/valheim-core) is installed, the host's curve applies to
everyone connected, in memory only, and your own config file comes back the moment you
disconnect. That is the half of it worth having on a server: without it, the reach is an
agreement between players rather than a property of the world, and anyone can set their own
radius to a hundred.

**On a Core server it goes on the server too, or on nobody.** Skaft registers with Core's gate
as host-only, which is why a client *without* it is let in. The reverse does not follow, and
the difference is worth knowing before you install this on a character that plays somewhere
else. Each end sends the other its list of mods, and that list does carry the host-only mark -
but the end reading it throws that field away and enforces its own view instead, so a mod on
the far end and not on this one is a refused connection whatever it was marked as. A server
that runs Core but not Skaft therefore turns away everyone who has it, with the game's stock
incompatible-version screen. Servers without Core are unaffected, because there is no gate to
refuse anything.

Nothing about this can be enforced server side, and it is worth being honest about why. The
game's repair message carries no permission check at all, and the server forwards it without
looking at who sent it, so any client has always been able to repair anything loaded. Checking
a radius on the server would mean inventing a second protocol to constrain something that was
never constrained.

## Known gaps

- **Singleplayer is the only place this has ever run.** One live world, 28 August 2026: at
  Crafting 55 the reach measured 7.5m, which is what the curve predicts, and one swing at a
  damaged wall repaired the two damaged walls beside it and left the intact ones alone. The
  radius, the trigger, the health filter and the per-piece charge are confirmed there and
  nowhere else.
- **No second player has ever seen it.** Not a dedicated server, not a guest, not another
  player's building. The config sync and the version gate are Core's and are exercised by
  other mods; the sweep itself has never been swung with anybody watching.
- **Wards are untested.** The argument in *Using it* is that the sweep inherits every check
  vanilla makes, wards included, because it only ever runs on a piece vanilla has just
  repaired. That is a reason to expect it to be right, not a report of it being right.
- **Running out mid-sweep is untested.** An empty stamina bar or a hammer arriving at
  `DurabilityFloor` part-way through one swing stops the sweep by construction. Neither stop
  has been watched happen.
- **These defaults are the ones you keep.** BepInEx writes every setting to disk the first
  time the mod loads and the saved value beats any later default in code, so retuning the
  curve in a future version reaches nobody who already has this one. The numbers are reasoned
  and measured at a single point on the curve; they have not been played from 0 to 60.

## Licence

MIT. See `LICENSE`.
