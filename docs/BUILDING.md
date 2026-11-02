# Building and checking the mod

## Prerequisites

- A local Windows Steam installation of Can ’Em All.
- MelonLoader 0.7.3 installed locally; launch once to generate IL2CPP references, then close the game.
- .NET SDK 8. The mod targets net6.0; core checks target net8.0.
- Python 3.11 with `tools/requirements.txt` installed in a virtual environment.

## Build script

```powershell
.\scripts\build.ps1 -GameDir "<your game directory>" -Python .\.venv\Scripts\python.exe
```

`-DotNet` accepts an alternate SDK executable. `-SkipGeometry` reuses an existing local snapshot. No game process is started, no files are installed into the game and no save is modified by the build script.

## Manual steps

```powershell
.\.venv\Scripts\python.exe tools/export_floor_geometry.py --game-dir "<your game directory>"
dotnet build src/PermanentUpgradesRevamped -c Release "-p:GameDir=<your game directory>"
```

You can also set the `CANEMALL_GAME_DIR` environment variable. Generated `FloorGeometry.json`, build output and all game/loader binaries are ignored by Git. Do not force-add them to a public commit or treat the resulting local DLL as a prepared public release.

## Core checks

```powershell
dotnet run --project tests/CoreChecks -c Release
```

These checks exercise balance, refunds, save migrations, batch-purchase dependency ordering, probability caps and hit-impulse rules without loading Unity or using a player save. Newtonsoft.Json is normally restored from NuGet. For an offline developer setup, supply a local assembly with `-p:NewtonsoftJsonPath=<path to Newtonsoft.Json.dll>`.

The GitHub workflow runs these core checks only; it does not download, build or distribute the game, loader or generated geometry. Gameplay physics, rendering, save round trips inside the game and performance need separate manual validation.
