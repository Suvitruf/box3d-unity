@echo off
setlocal

rem Builds box3d.dll (x64, Release, single precision) and copies it into the package.
rem Requires Visual Studio 2022 (or Build Tools) and CMake.
rem
rem Environment (optional):
rem   BOX3D_SRC  box3d checkout; auto-probed next to the repo if unset
rem   CMAKE      cmake executable; found on PATH or in VS Build Tools if unset

if not defined BOX3D_SRC if exist "%~dp0..\..\box3d\include\box3d\box3d.h" set "BOX3D_SRC=%~dp0..\..\box3d"
if not defined BOX3D_SRC if exist "%~dp0..\..\..\..\box3d\include\box3d\box3d.h" set "BOX3D_SRC=%~dp0..\..\..\..\box3d"
if not defined BOX3D_SRC (
  echo error: box3d checkout not found - set BOX3D_SRC
  exit /b 1
)

if not defined CMAKE where cmake >nul 2>nul && set "CMAKE=cmake"
if not defined CMAKE set "CMAKE=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"

set BUILD_DIR=%~dp0build
set OUT_DIR=%~dp0..\Plugins\Windows\x86_64

"%CMAKE%" -S "%BOX3D_SRC%" -B "%BUILD_DIR%" -G "Visual Studio 17 2022" -A x64 ^
  -DBUILD_SHARED_LIBS=ON ^
  -DBOX3D_SAMPLES=OFF ^
  -DBOX3D_UNIT_TESTS=OFF ^
  -DBOX3D_BENCHMARKS=OFF ^
  || exit /b 1

"%CMAKE%" --build "%BUILD_DIR%" --config Release || exit /b 1

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
copy /Y "%BUILD_DIR%\bin\Release\box3d.dll" "%OUT_DIR%\box3d.dll" || exit /b 1

echo Done: %OUT_DIR%\box3d.dll
