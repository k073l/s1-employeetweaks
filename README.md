# EmployeeTweaks

A collection of employee-related tweaks.

## Features
- Automated **unpackaging**!
   - Designate a packaging station as unpackaging station using a **clipboard menu**
   - Set a **route** for your **handler** **from a storage entity** like a storage rack or a loading bay **to the station**
   - Set a **destination for the station**, such as a storage rack or a mixing station
   - The handler will **automatically pick up packaged product**, **unpack it** at the station, and **deliver the unpackaged product** to the destination

![Clipboard station configuration](https://raw.githubusercontent.com/k073l/s1-employeetweaks/refs/heads/master/assets/screenshots/clipboard.png)

![Handler unpackaging bricks](https://raw.githubusercontent.com/k073l/s1-employeetweaks/refs/heads/master/assets/screenshots/unpackaging.png)

- Botanists can now use equipment such as **Soil Pourers**, **Pot Sprinklers** and **Big Sprinklers**, greatly improving their efficiency

![Botanist using Soil Pourer](https://raw.githubusercontent.com/k073l/s1-employeetweaks/refs/heads/master/assets/screenshots/soilpourer.png)

![Botanist using Pot Sprinkler](https://raw.githubusercontent.com/k073l/s1-employeetweaks/refs/heads/master/assets/screenshots/sprinkler.png)

- Quality is now take into account when employees are consuming product
   - For `Athletic`, `Energizing` and `Focused` higher quality will provide a greater boost than base game, while lower quality will provide a lower boost than base game
   - For `Sedating` higher quality will reduce the penalty, while lower quality will increase the penalty
- New Filter option in the Filter dropdown menu - `Apply Item to Filter`
   - When this option is selected, the current item in the slot will be applied as a allowlist filter for that slot
   - Holding `Shift` while clicking will change it to `Apply All to Filters`, which will apply all current items as filters for their respective slots
   - Holding `Ctrl` while clicking will cause the filter to be applied as a denylist filter instead of an allowlist filter

![Filter dropdown with Apply All to Filters option](https://raw.githubusercontent.com/k073l/s1-employeetweaks/refs/heads/master/assets/screenshots/applyfilter.png)

- Preferences for adjusting employee capacity limits per property at runtime
   - Configurable with any mod preferences manager solution, such as [ModsApp](https://new.thunderstore.io/c/schedule-i/p/k0Mods/ModsApp/), allows for easy adjustment when you need one more employee slot for a Cleaner

![Customizable capacities shown in ModsApp](https://raw.githubusercontent.com/k073l/s1-employeetweaks/refs/heads/master/assets/screenshots/capacities.png)

## Installation
1. Install MelonLoader
2. Extract the zip file
3. Place the dll file into the Mods directory for your branch
    - For none/beta use IL2CPP
    - For alternate/alternate beta use Mono
4. Install S1API (Forked)
5. Launch the game

## License
This mod is licensed under MIT License. See the LICENSE.md file for more information.
