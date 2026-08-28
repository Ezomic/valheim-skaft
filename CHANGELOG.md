# Changelog

## 0.1.0 - unreleased

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
singleplayer world.

Not yet exercised: wards, another player's pieces, running out of stamina or durability
mid-sweep, and anything at all in multiplayer.
