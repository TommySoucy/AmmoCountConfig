# AmmoCountConfig
A mod for JustEmuTarkov that adds options for the display of a gun's current ammo count in raid. 
The mod can make it so the ammo count is always visible, with a specified ammo count approximation precision of your choosing, 
and can even add more details the game doesn't usually display with ammo count, like whether you have a round in the chamber and the max amount of rounds in the current magazine.

This mod's config is very important to customize how it works but by default:

 - The ammo count will now **always** be shown. No need to check the mag anymore!
 - The **max ammount** of ammo that can be in the mag is also displayed.
 - The amount of rounds in the **chamber** will also be displayed. No need to check the chamber anymore!
 - The ammo count approximation precision will always be at maximum. So even if your Mag Drills skill level is 0, you will still know **exactly** the amount of ammo you have left in the mag!
 - All this can be changed in the config! How? See Config section below.
 
 So by default, if you have 5 rounds left in a 10 round mag and the gun has a round in the chamber, the ammo count displayed will be "5 / 10 +1". 
 The "/ 10" indicates how much ammo maximum can be in the mag, and the "+1" says there is a round in the chamber.

![alt text](https://github.com/TommySoucy/AmmoCountConfig/blob/master/hub/example0.png "Full Example")
![alt text](https://github.com/TommySoucy/AmmoCountConfig/blob/master/hub/example1.png "Some Example")
![alt text](https://github.com/TommySoucy/AmmoCountConfig/blob/master/hub/example2.png "Empty Example")

## Installation

1. Download latest from [releases](https://github.com/TommySoucy/AmmoCountConfig/releases)
2. Download the latest MelonLoader installer from [here](https://github.com/LavaGang/MelonLoader/releases) and install version **_0.3.0_** into your JET installation
3. Put all files from the .zip into the Mods folder created by MelonLoader

## Config

- **_alwaysShow_**: This setting will decide whether to always show the ammo count or not.

- **_fireModeDelay_**: When checking/changing the fire mode, this is how long it will show the fire mode in place of the ammo count, in seconds. This delay is only relevant if alwaysShow is set to true. It will otherwise use game default.

- **_zeroingDelay_**: When aiming down sight or checking/changing zero distance, this is how long it will show the zero distance in place of the ammo count, in seconds. This delay is only relevant if alwaysShow is set to true. It will otherwise use game default.

- **_level_**: This level decides how precise the ammo count shown is. In game, the mag drill level is used to decide how precise of a count we usually get. This setting only takes effect if it is set higher than the original Mag Drills level in game or if forceLevel setting is set to true. Setting level to -1 will keep level to what it is in game.
  * At any lvl, will show full or empty if thats the case.
  * At lvl 0, will give an approx. Almost full, Less than half, etc.
  * At lvl 1, will give the exact amount if under MAX ammo count is under 10, a close approx. if ammo count is above 5, and "less than 5" if that's the case.
  * At lvl 2 and above, will give exact amount.

- **_forceLevel_**: This setting decides whether to force the above level setting or not. The level is usually only set to the set value if it is higher than the original in-game Mag Drills level (because doesn't have an effect anyway otherwise). This will force the level to be set to the level setting one even if it is lower than the in-game one.

- **_showMax_**: In game, a ammo count is usually shown as a single number, without showing the maximum. For example, if you have a mag with size of 30 rounds in your gun, but only has 15 rounds in it, and you check it, the ammo count shown will be "15". If the following setting is set to true, it will instead show "15/30".

- **_showChamber_**: By default, you need to check the chamber to know if you still have a round in there. If this is set to true, it will show how much ammo you have in the chamber. For example, if you have 1 round in chamber in will show a "+1" after the count. Once the chamber is empty, it will show "+0".

## Building

1. Clone repo
2. Open solution
3. Ensure all references are there
4. Build
5. Find built dll and open it in dnSpy
6. Right click and "Edit IL Code" any lines that call a method in "(__instance as EFT.UI.UIElement)" in the source code. This is meant to be a call to a method in "base"
7. On the left of this line, you want it to show "call" but it will now be "callvirt" instead, click on that and change it to "call"
8. Save module. DLL is now ready for install as explained in **Installation** section

## Used libraries

- Harmony
- MelonLoader
