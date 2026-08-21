@echo off
setlocal EnableExtensions
rem Standalone KH38MT: AAM-36 single rail + Kh-85MT mesh + AGM-68 warhead + Mach 8
rem Independent of Oritasy / WeXon. Installs even when Oritasy_*.dll is present.
if defined KH38_GAME (
  set "GAME=%KH38_GAME%"
) else (
  set "GAME=d:\Steam\steamapps\common\Nuclear Option"
)
set MANAGED=%GAME%\NuclearOption_Data\Managed
set BEP=%GAME%\BepInEx\core
set PLUGINS=%GAME%\BepInEx\plugins
set OUT=%~dp0KH38MT.dll
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ASSETS_SRC=%~dp0assets
set ASSETS_DST=%PLUGINS%\KH38MTAssets

if not exist "%CSC%" (
  echo csc.exe not found
  exit /b 1
)
if not exist "%MANAGED%\Assembly-CSharp.dll" (
  echo Assembly-CSharp.dll not found under GAME path
  exit /b 1
)

"%CSC%" /noconfig /nostdlib /nologo /optimize+ /target:library /platform:anycpu /langversion:5 ^
  /out:"%OUT%" ^
  /r:"%MANAGED%\mscorlib.dll" ^
  /r:"%MANAGED%\netstandard.dll" ^
  /r:"%MANAGED%\System.dll" ^
  /r:"%MANAGED%\System.Core.dll" ^
  /r:"%BEP%\BepInEx.dll" ^
  /r:"%BEP%\0Harmony.dll" ^
  /r:"%MANAGED%\Assembly-CSharp.dll" ^
  /r:"%MANAGED%\Mirage.dll" ^
  /r:"%MANAGED%\UnityEngine.CoreModule.dll" ^
  /r:"%MANAGED%\UnityEngine.dll" ^
  /r:"%MANAGED%\UnityEngine.PhysicsModule.dll" ^
  /r:"%MANAGED%\UnityEngine.ImageConversionModule.dll" ^
  /r:"%MANAGED%\UnityEngine.IMGUIModule.dll" ^
  "%~dp0src\Plugin.cs" ^
  "%~dp0src\Kh38MtVisual.cs" ^
  "%~dp0src\Kh38MtWeapon.cs" ^
  "%~dp0src\Kh38MtPatches.cs"

if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)

echo Built: %OUT%

if not exist "%ASSETS_DST%" mkdir "%ASSETS_DST%"
for %%A in (Kh-85MT.obj su_kh_38mt.mtl su_kh38_mt_missile_c.jpg Kh-85MT_icon.png) do (
  if exist "%ASSETS_SRC%\%%A" copy /Y "%ASSETS_SRC%\%%A" "%ASSETS_DST%\" >nul
)
if not exist "%PLUGINS%\WeXonAssets" mkdir "%PLUGINS%\WeXonAssets"
if exist "%ASSETS_SRC%\Kh-85MT_icon.png" copy /Y "%ASSETS_SRC%\Kh-85MT_icon.png" "%PLUGINS%\WeXonAssets\" >nul

if exist "%PLUGINS%\KH38MT.dll" (
  del /f /q "%PLUGINS%\KH38MT.dll" 2>nul
  if exist "%PLUGINS%\KH38MT.dll" ren "%PLUGINS%\KH38MT.dll" KH38MT.dll.old 2>nul
)
copy /Y "%OUT%" "%PLUGINS%\KH38MT.dll"
if errorlevel 1 (
  echo INSTALL FAILED - close the game and rebuild
  exit /b 1
)
del /f /q "%PLUGINS%\KH38MT.dll.old" 2>nul

echo Installed: %PLUGINS%\KH38MT.dll
echo Assets:    %ASSETS_DST%
echo Config:    BepInEx\config\com.iallemege.kh38mt.cfg
endlocal
