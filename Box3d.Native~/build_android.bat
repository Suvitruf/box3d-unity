@echo off
setlocal

rem Builds libbox3d.so for Android arm64-v8a using Unity's bundled NDK.
rem
rem Environment:
rem   UNITY_EDITOR  Unity editor folder containing Data\PlaybackEngines (probed if unset)
rem   BOX3D_SRC     box3d checkout; auto-probed next to the repo if unset
rem   CMAKE         cmake executable; PATH or VS Build Tools if unset
rem   NINJA         ninja executable; PATH or VS Build Tools if unset

if not defined BOX3D_SRC if exist "%~dp0..\..\box3d\include\box3d\box3d.h" set "BOX3D_SRC=%~dp0..\..\box3d"
if not defined BOX3D_SRC if exist "%~dp0..\..\..\..\box3d\include\box3d\box3d.h" set "BOX3D_SRC=%~dp0..\..\..\..\box3d"
if not defined BOX3D_SRC (
  echo error: box3d checkout not found - set BOX3D_SRC
  exit /b 1
)

if not defined UNITY_EDITOR (
  for /d %%v in ("C:\Program Files\Unity\Hub\Editor\*") do (
    if exist "%%v\Editor\Data\PlaybackEngines\AndroidPlayer\NDK\source.properties" set "UNITY_EDITOR=%%v\Editor"
  )
)
if not defined UNITY_EDITOR (
  echo error: no Unity install with Android NDK found - set UNITY_EDITOR
  exit /b 1
)
set "NDK=%UNITY_EDITOR%\Data\PlaybackEngines\AndroidPlayer\NDK"

if not defined CMAKE where cmake >nul 2>nul && set "CMAKE=cmake"
if not defined CMAKE set "CMAKE=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
if not defined NINJA where ninja >nul 2>nul && set "NINJA=ninja"
if not defined NINJA set "NINJA=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe"

set BUILD_DIR=%~dp0build-android
set OUT_DIR=%~dp0..\Plugins\Android\arm64-v8a

"%CMAKE%" -S "%BOX3D_SRC%" -B "%BUILD_DIR%" -G Ninja ^
  -DCMAKE_MAKE_PROGRAM="%NINJA%" ^
  -DCMAKE_TOOLCHAIN_FILE="%NDK%\build\cmake\android.toolchain.cmake" ^
  -DANDROID_ABI=arm64-v8a ^
  -DANDROID_PLATFORM=android-23 ^
  -DCMAKE_BUILD_TYPE=Release ^
  -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" ^
  -DBUILD_SHARED_LIBS=ON ^
  -DBOX3D_SAMPLES=OFF ^
  -DBOX3D_UNIT_TESTS=OFF ^
  -DBOX3D_BENCHMARKS=OFF ^
  || exit /b 1

"%CMAKE%" --build "%BUILD_DIR%" || exit /b 1

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
copy /Y "%BUILD_DIR%\bin\libbox3d.so" "%OUT_DIR%\libbox3d.so" || exit /b 1
"%NDK%\toolchains\llvm\prebuilt\windows-x86_64\bin\llvm-strip.exe" --strip-unneeded "%OUT_DIR%\libbox3d.so"

echo Done: %OUT_DIR%\libbox3d.so
