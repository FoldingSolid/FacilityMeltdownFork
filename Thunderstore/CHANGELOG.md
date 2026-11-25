## v2.7.5
- Migrated the fork to Lethal Company v81 and the current DawnLib content API.
- Fixed the v81 Netcode RPC registration issue that could stop a meltdown immediately after the apparatus was removed.
- Added the current package assets, translations, SoundAPI mappings, newer meltdown protections, and the Mirage masked-enemy compatibility fix.
- Added optional Diomedes bingo challenges and loaforc's SoundAPI conditions.
- Fixed optional WeatherRegistry handling so its absence cannot block a meltdown, and aligned apparatus setup with the current source.
- Fixed particle-effect placement and settings, warning-audio volume scaling, English translation fallback, and Geiger/radiation edge cases.
- Added an optional HUD countdown and made the configurable meltdown duration work correctly. The host's selected duration is sent to every player; terminal scans, dialogue, progress effects, and the explosion remain in sync.
- Added escalating screen shake during the second half of the countdown, separate from random environmental shakes.
- Consolidated every setting into `me.loaforc.facilitymeltdown.cfg`, with one-time import of existing client settings.
- Replaced the fixed apparatus-value override with deterministic inclusive minimum and maximum values.
- Removed the obsolete LethalSettings integration and legacy client-config split.

## v2.6
- Entire rewrite of the config system.
- Meltdown now entirely handles config syncing, CSync is no longer a dependency.

### v2.6.1
- Added more protection if meltdown fails to setup some effects.
- Fixed meltdown starting twice ig?

### v2.6.2
- Fixed accidently clearing client configs.

### v2.6.3
- Fixed Geiger Counter
- Fixed ship and map props lights turning red.
- Changed how the outside lights flash, should no longer be an all consuming void. 

### v2.6.4
- Hopefully fixed some windows locales being unable to load meltdown.

### v2.6.5
- Fixed explosion particles being pink.
- Added a new config option, `MinPeopleToPullApparatus`. Default is 2. Requires at least this many people. If this number is larger than the amount of people in the lobby then EVERYBODY will need to be nearby.
- Lobby Compatibility is no longer a "hard" dependency but will remain a dependency on thunderstore. If it is causing issues you can disable it and meltdown will still work.

### v2.6.6
- Fixed MinPeopleToPullAPparatus not taking into account dead people :skull:

### v2.6.7
- Fixed Config not syncing correctly.

### v2.6.8
- Applied Hotfix on GeneralImprovements to unbreak the tooltip when you need more people to pull the apparatus.
- `MinPeopleToPullApparatus` now only applies to 9PM. After 9PM anybody can grab the apparatus, regardless of how many people are nearby.

### v2.6.9
- Removed GeneralImprovements hotfix as it was fixed in v1.2.7 :3

### v2.6.10
- Reapplied the hotfix LMAO.

### v2.6.11
- Undid the reapplied hotfix. :skull:

### v2.6.12
- v55 compatibility
- Minimised stutter when starting the meltdown sequence.

### v2.6.13
- git moment

### v2.6.14
- added intergration with weather registry, toggleable in config
- fixed geiger counter having no battery

### v2.6.15
- oops
