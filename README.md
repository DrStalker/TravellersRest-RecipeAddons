# TravellersRest-RecipeAddons

A Traveller's Rest mod to see how far I can push recipe changes without adding any new items to the game.

##Current features:

* Convert a specific type of item into a generic selectable (example: use any juice in recipes that use juice)

* Add an extra item to an existing Recipe (Examples: cheeseburger with sauce, porrige with fruit)

* Add an exta item type to an IngredientGroup (example: Fruit and Vege are interchangable)

* Change ingrediant groups (example: use any malt or any hops when brewing)

* Add a new recipe: Doesn't actually work yet, but doesn't breeak anything either (non-fucntional example: craft decorative barrels)


##Planned in the future:

Make ingredient generic: A function that is passed a recipe and ingrediant, and all instances of that ingrediat are replaced with a generic group (e.g.: Function(Vodka, potato) will allow vodka to be made from any vegetable) 

Swap ingredients: A function that is passed a a recipe and two ingredients, and swaps ingrediant one for ingredient two (e.g. Functiopn(Salad, Lettuce, cabbage) will let you simulate the Great Lettuce Shortage of 2023 when restaurents tried to get away with using cabbage instead of lettuce due to pricing.)

Change Output Amount: a Function to change the amount that a recipe produces.

Add recipe: get this to work, instead of just popping up "new recipe added" when loading a game but not being usable.


##A note for any modders building on this:

From quick testing the UI will break if a recipe has more than five ingredients or more than three selectable ingredients.


##Downloading the mod

Mods are available on [Nexus Mods](https://www.nexusmods.com/travellersrest) or you can download the mod from [compiled-releases](https://github.com/DrStalker/TravellersRest-RecipeAddons/tree/main/compiled-releases)


##How to install mods:

* Install [Bepinex](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2)﻿ (Stable version 5.4 for Windows 64; the filename will look like `BepInEx_win_x64_5.4.23.2.zip`)
* Start the game, quit the game after it finishes loading
* This will create a Bepinex config file and a plugins folder that you can put additional mods in
* (optional) Enable the Bepinex Console (see the detailed guide or the Bepinex documentation for steps)
* Copy the mod .dll to the plugins directory.


## How to change mod settings:

* Install the mod and start the game.
* Bepinex will create a file in the \BepInEx\config\ with default settings for the mod.
* Exit the game, edit the config file, restart the game.


## Is this mod save to add/remove mid play-through?

This mod can be added to a existing save, but with some quirks; If you adjust recipes to allow modifiers where there were previously none the game will pick an item for the missing modifier, so all your existing wine will be rasberry flavored because the juice modifier was missing. nothing breaks, you'll just have some odd foods/drinks.


## Traveller's Rest Modding Guide

﻿[Here are my notes on modding Traveler's Rest.](https://docs.google.com/document/d/e/2PACX-1vSciLNh4KgUxE4L2h_K0KAxi2hE6Z1rhroX0DJVhZIqNEgz2RvYESqffRl8GFONKKF1MjYIIGI5OKHE/pub)

