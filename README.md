# MIG-15S

Nuclear Option (0.34) BepInEx pack: Aryx MiG-15 turned into the **MIG-15S** kamikaze drone, plus standalone **KH38MT**.

Visual airframe is Aryx's MiG-15 1.1.1. This pack only changes how it flies and what it can hang.

## Install

1. [BepInEx 5](https://github.com/BepInEx/BepInEx) already in the game folder.
2. Blueprinter in `BepInEx/plugins` (required for the `.nobp` airframe).
3. Download **MIG-15S-1.0.0.zip** from [Releases](https://github.com/iallemege/MIG-15S/releases).
4. Extract into the Nuclear Option game folder so files land in `BepInEx/plugins/`.
5. Fully quit Steam, then launch the game.

## MIG-15S (`com.ial.mig15s`)

- Hangar name **MIG-15S**. Suicide fuze, no 23/37/57 mm guns.
- Third pylon can take **KH38MT**. Other pylons keep the stripped catalog (tailhook).
- Unrestricted-weapon mods do not dump extra stores onto this airframe.

## KH38MT (`com.iallemege.kh38mt`)

Standalone missile. Does not need Oritasy.

- AAM-36 single rail, one round, **third hardpoint only**
- AGM-68-class warhead, Mach 8
- Aircraft pylons only (not ships)

## Build from source

Windows, `csc` from .NET Framework 4.x. Set the game path if it is not the default Steam folder.

```bat
cd MiG15S
build.bat

cd ..\KH38MT
build.bat
```

`MiG15S\build.bat` also copies `MiG-15S.nobp` from the Aryx 1.1.1 bundle when that zip is present locally. The GitHub Release already includes the `.nobp`.

## Credits

- Airframe mesh/textures: **Aryx** (MiG-15 1.1.1)
- Runtime plugins: IAL / iallemege
