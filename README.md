# Facility Meltdown Fork

The apparatus was vital to the facility reactor—and you just took it out.

Removing it begins a facility-wide self-destruct sequence. Escape to the ship before the reactor explodes, or be caught in the fireball. The default countdown is two minutes, but the host can configure it from 60 to 300 seconds.

This fork is migrated for Lethal Company v81 and is intended as a drop-in replacement for the original Facility Meltdown. It works with vanilla content, most custom moons, and interiors.

### A modded moon turns red

Some custom moon creators do not set up lighting in the same way as vanilla moons. That can make a meltdown tint the moon red. The moon needs explicit support from its creator; Facility Meltdown cannot safely correct every custom lighting setup.

## Credits
Music and voice lines come from portal 2. I love portal 2.

## What this mod adds
- A configurable meltdown sequence with emergency lights, warning announcements, interior failures, bonus enemy spawns, and a final fireball.
- An optional HUD countdown. It uses the host-selected duration and reaches zero at detonation.
- Escalating tension shakes during the second half of a meltdown, in addition to the existing intermittent environmental shakes.
- A Geiger Counter available from the terminal for 90 credits. Use it to find radiation sources, including the apparatus.
- The `reactor health` terminal command, which reports remaining time and reactor instability.
- A Mirage compatibility fix: unsupported masked-enemy entries are ignored safely instead of preventing the sequence from starting.

## Custom sounds

[loaforc's SoundAPI](https://thunderstore.io/c/lethal-company/p/loaforc/loaforcsSoundAPI/) is optional. When installed, it can replace the meltdown music and warning voices through the included mappings and exposes meltdown-state conditions for sound packs. The fork includes its normal music, announcements, shockwave, and explosion sounds without SoundAPI.

### with Diomedes
[Diomedes](https://thunderstore.io/c/lethal-company/p/DiomedesLC/Diomedes/) is optional. When present, FacilityMeltdown adds bingo challenges for triggering a meltdown on any moon, Embrion, Artifice, or Titan.

## Config
All Facility Meltdown settings live in one file: `me.loaforc.facilitymeltdown.cfg`. On first launch after upgrading, values from the former `me.loaforc.facilitymeltdown.Client.cfg` are copied into that file; the old file is then unused and can be removed.

Set `ApparatusValueMin` and `ApparatusValueMax` to choose an inclusive apparatus-value range. The selected value is deterministic for that moon, so every player receives the same value. Set both values to the same number to keep a fixed value. WeatherRegistry multiplication, if enabled, is applied afterward.

The file also contains bonus enemy spawns, emergency lights, required nearby players, Maneater behavior, ship-departure countdown shortening, excluded enemies, reactor-health scans, WeatherRegistry value multiplication, meltdown duration, music/announcement volume, outside music, camera shake, particles, the optional countdown HUD, and language. Changes to the duration apply to the next meltdown; every player receives that chosen duration when it starts.

### Optional in-game config editor

[LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) lets you edit the settings in-game. LethalSettings is not supported by this build.

### Tips and Tricks
- Plan your escape route before pulling the apparatus.
- A player at the ship can use `reactor health` to monitor the remaining time.
- Facility failures intensify as the reactor destabilizes; the later countdown is deliberately more violent.
- The ship is safe from the explosion and automatically leaves at detonation. Be aboard before then.
