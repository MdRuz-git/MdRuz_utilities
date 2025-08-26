# MdRuz_utilities
Rimworld 1.6/1.5 mod
TL;DR: automates some tasks, such as: medicine switching, drug policy, zone switching. Introduces 'Bill' persistence if building is rebuilt it will keep its bills intact. Revamps almost all psycasts, and more.

more info:
-bills inside production buildings will never be lost if that building got destroyed by raiders. (enable autorebuild in the game UI -> bottom right (hammer icon))

-deepscanner will now prioritize discovering ores around itself in a 80x80 grid. works with multiple scanners.
however if the spot for ore vein is somehow incorrect or already occupied it will fall back to vanilla which has no range limit.

-adds a button to AREA, manage area window, which will:
assign that area to a pair of 2.
automatically switch between chosen 2 areas when any ENEMIES are present on the map.
it only switches the area of pawns which are already assigned between the area pairing. (so if you have more than 2 areas assigned, colonists that are not assigned either or, of those 2 areas wont be changed.)

-automatically switch to 'industrial medicine' or better for colonists with disease/infection
developed immunity to an infection, switches back to 'doctor care but no medicine'
this function is performed only once, so that the player can switch their preference back to whatever at anytime.

-pawns below 50% mood lose certainty faith or belief in their ideo (instead of vanilla where they always gain it no matter what)
resulting in prisoners moving towards crisis of belief when unhappy. makes tiny easier to convert them.
it also applies to colonists but that by itself does NOT have any effect

-new ritual reward which replaces Random Recruit. Taunt enemies. 60% chance of triggering a very small raid to attack your colony.

-colonists inside caskets are not counted for colony wealth calculation. should support any building where a pawn can go into

-slaves counted as 50% of pawn value. (instead of vanilla 75%)

