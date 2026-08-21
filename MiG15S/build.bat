@echo off
setlocal EnableExtensions
rem Standalone MiG-15S (Blueprinter .nobp + fuze / 10kt / TWR2 runtime)
set "GAME=d:\Steam\steamapps\common\Nuclear Option"
set MANAGED=%GAME%\NuclearOption_Data\Managed
set BEP=%GAME%\BepInEx\core
set PLUGINS=%GAME%\BepInEx\plugins
set OUT=%~dp0MiG15S.dll
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set NOBP_SRC=g:\8\aryx.mig15.kamikaze\Aryx.s.MiG-15_Kamikaze.nobp
set NOBP_ZIP=g:\8\aryx.mig15.zip

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
  /r:"%MANAGED%\UnityEngine.IMGUIModule.dll" ^
  /r:"%MANAGED%\UnityEngine.TextRenderingModule.dll" ^
  /r:"%MANAGED%\UnityEngine.InputLegacyModule.dll" ^
  /r:"%MANAGED%\UnityEngine.UI.dll" ^
  "%~dp0src\Plugin.cs" ^
  "%~dp0src\Service.cs" ^
  "%~dp0src\Hangar.cs" ^
  "%~dp0src\LoadoutLock.cs"

if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)

echo Built: %OUT%

if exist "%PLUGINS%\MiG15S.dll" (
  del /f /q "%PLUGINS%\MiG15S.dll" 2>nul
  if exist "%PLUGINS%\MiG15S.dll" ren "%PLUGINS%\MiG15S.dll" MiG15S.dll.old 2>nul
)
copy /Y "%OUT%" "%PLUGINS%\MiG15S.dll"
if errorlevel 1 (
  echo INSTALL FAILED - close the game and rebuild
  exit /b 1
)
del /f /q "%PLUGINS%\MiG15S.dll.old" 2>nul

if exist "%NOBP_SRC%" (
  copy /Y "%NOBP_SRC%" "%PLUGINS%\MiG-15S.nobp"
  if exist "%PLUGINS%\Aryx.s.MiG-15_Kamikaze.nobp" del /f /q "%PLUGINS%\Aryx.s.MiG-15_Kamikaze.nobp"
) else if exist "%NOBP_ZIP%" (
  echo Extracting original Aryx MiG-15 textures from zip
  python -c "import zipfile,shutil; z=zipfile.ZipFile(r'%NOBP_ZIP%'); open(r'%PLUGINS%\MiG-15S.nobp','wb').write(z.read('aryx.mig15/Aryx.s.MiG-15_1.1.1.nobp'))"
)

echo Installed: %PLUGINS%\MiG15S.dll
echo Aircraft:  %PLUGINS%\MiG-15S.nobp
echo Needs:     Blueprinter_1.8.17.dll in plugins
endlocal
