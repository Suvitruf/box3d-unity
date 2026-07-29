@echo off
setlocal

rem Builds box3d.dll (x64, Release, single precision) and copies it into the package.
rem Requires Visual Studio 2022 (or Build Tools) and CMake.
rem
rem Environment (optional):
rem   BOX3D_SRC     box3d checkout; auto-probed next to the repo if unset
rem   CMAKE         cmake executable; found on PATH or in VS Build Tools if unset
rem   GENERATOR     cmake generator; defaults to the VS 2022 generator. Set to "Ninja" (with the MSVC
rem                 dev env activated, e.g. on CI) when the VS generator can't locate an instance.
rem   BOX3D_DOUBLE  set (any value) to build the DOUBLE-precision variant -> box3d_d.dll
rem                 (distinct name; the C# BOX3D_DOUBLE define selects it). Unset = single (default).

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
if not defined GENERATOR set "GENERATOR=Visual Studio 17 2022"

set "PREC_FLAG="
set "SUFFIX="
if defined BOX3D_DOUBLE (
  set "PREC_FLAG=-DBOX3D_DOUBLE_PRECISION=ON"
  set "SUFFIX=_d"
  set "BUILD_DIR=%~dp0build-double"
)

if /I "%GENERATOR%"=="Ninja" (
  "%CMAKE%" -S "%BOX3D_SRC%" -B "%BUILD_DIR%" -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DBOX3D_SAMPLES=OFF -DBOX3D_UNIT_TESTS=OFF -DBOX3D_BENCHMARKS=OFF %PREC_FLAG% || exit /b 1
  "%CMAKE%" --build "%BUILD_DIR%" || exit /b 1
) else (
  "%CMAKE%" -S "%BOX3D_SRC%" -B "%BUILD_DIR%" -G "%GENERATOR%" -A x64 -DBUILD_SHARED_LIBS=ON -DBOX3D_SAMPLES=OFF -DBOX3D_UNIT_TESTS=OFF -DBOX3D_BENCHMARKS=OFF %PREC_FLAG% || exit /b 1
  "%CMAKE%" --build "%BUILD_DIR%" --config Release || exit /b 1
)

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
rem VS puts the dll under bin\Release; single-config Ninja puts it under bin.
copy /Y "%BUILD_DIR%\bin\Release\box3d.dll" "%OUT_DIR%\box3d%SUFFIX%.dll" 2>nul || copy /Y "%BUILD_DIR%\bin\box3d.dll" "%OUT_DIR%\box3d%SUFFIX%.dll" || exit /b 1

echo Done: %OUT_DIR%\box3d%SUFFIX%.dll
