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

# Reproducible output on MSVC: link.exe otherwise stamps the current time into the COFF header
# and the export directory, so every CI run yields a "new" box3d.dll even when nothing changed
# and the build-natives workflow commits it. /Brepro replaces both stamps with a content hash.
if(MSVC)
    target_link_options(box3d PRIVATE /Brepro)
endif()

# v0.1.0 leaks the cloned vertices/indices when mesh bounds are invalid (e.g. all
# triangles degenerate after welding). Patch only a build-local translation unit.
file(READ "${BOX3D_SRC}/src/mesh.c" mesh_source)
string(REPLACE "\r\n" "\n" mesh_source "${mesh_source}")
set(mesh_failure "\tif ( b3IsSaneAABB( meshBounds ) == false )\n\t{\n\t\tb3Array_Destroy( primitives );")
set(mesh_fixed "${mesh_failure}\n\t\tb3Array_Destroy( indices );\n\t\tb3Array_Destroy( vertices );")
string(FIND "${mesh_source}" "${mesh_fixed}" already_fixed)
if(already_fixed EQUAL -1)
    string(FIND "${mesh_source}" "${mesh_failure}" failure_position)
    if(failure_position EQUAL -1)
        message(FATAL_ERROR "Mesh cleanup patch no longer matches Box3D; review the upstream failure path")
    endif()
    string(REPLACE "${mesh_failure}" "${mesh_fixed}" mesh_source "${mesh_source}")
endif()
file(WRITE "${CMAKE_CURRENT_BINARY_DIR}/mesh_fixed.c" "${mesh_source}")
get_target_property(box3d_sources box3d SOURCES)
list(REMOVE_ITEM box3d_sources mesh.c)
set_property(TARGET box3d PROPERTY SOURCES "${box3d_sources}")
target_include_directories(box3d PRIVATE "${BOX3D_SRC}/src")
target_sources(box3d PRIVATE "${CMAKE_CURRENT_BINARY_DIR}/mesh_fixed.c")

option(BOX3D_UNITY_TESTS "Build native Unity integration regression tests" OFF)
if(BOX3D_UNITY_TESTS)
    enable_testing()
    add_executable(mesh_cleanup_test mesh_cleanup_test.c)
    target_link_libraries(mesh_cleanup_test PRIVATE box3d)
    set_target_properties(mesh_cleanup_test PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/box3d/bin")
    add_test(NAME mesh_cleanup COMMAND mesh_cleanup_test)
endif()
