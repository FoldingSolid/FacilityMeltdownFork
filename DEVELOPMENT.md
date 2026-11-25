# Building FacilityMeltdown

This fork builds against a locally installed copy of the current Lethal Company
game and three mod libraries. The project defaults match the developer paths on
the machine where this migration was performed. Supply MSBuild properties when
building somewhere else:

```powershell
dotnet build FacilityMeltdown.sln -c Release `
  -p:GameManagedPath="C:\Program Files (x86)\Steam\steamapps\common\Lethal Company\Lethal Company_Data\Managed" `
  -p:DawnLibPath="C:\path\to\com.github.teamxiaolan.dawnlib.dll" `
  -p:DiomedesPath="C:\path\to\me.loaforc.diomedes.dll" `
  -p:SoundApiPath="C:\path\to\me.loaforc.soundapi.dll"
```

`DawnLib` is required at runtime. Diomedes and loaforc's SoundAPI are optional
at runtime: the integration methods are deliberately isolated so the base mod
loads when either is absent. Their DLLs remain compile-time references because
the optional integrations use their public APIs.

The build restores the repository's local NetcodePatcher tool and patches the
resulting `FacilityMeltdown.dll` automatically. Package it with `tcli build`
from the repository root after a successful Release build.

Before publishing, change the Thunderstore `namespace` and `websiteUrl` in
`thunderstore.toml` to identities you control; `loaforc` is retained only for
drop-in compatibility with existing installations.
