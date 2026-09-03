# Wrapper build: compiles the UNTOUCHED box3d checkout plus the Unity glue
# (box3d_unity_glue.c — pointer-only b3u_* exports for Burst) into one library.
# Used by every build_* script: cmake -S <this dir> -DBOX3D_SRC=<box3d checkout> [box3d options...]
# All box3d cache options (BUILD_SHARED_LIBS, BOX3D_SAMPLES, BOX3D_DOUBLE_PRECISION, ...) pass
# through unchanged. Outputs land under <build>/box3d/bin (box3d's own output-dir setup, scoped
# to its subdirectory).

cmake_minimum_required(VERSION 3.22)

project(box3d_unity LANGUAGES C CXX)

if(NOT DEFINED BOX3D_SRC)
    message(FATAL_ERROR "Pass -DBOX3D_SRC=<path to the box3d checkout>")
endif()

add_subdirectory("${BOX3D_SRC}" "${CMAKE_BINARY_DIR}/box3d")
target_sources(box3d PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/box3d_unity_glue.c")
