#include <box3d/box3d.h>
#include <box3d/collision.h>
#include <box3d/constants.h>
#include <stdio.h>

static int CheckFailure(const b3Vec3* vertices, bool weld)
{
    int indices[] = {0, 1, 2};
    b3MeshDef def = {0};
    def.vertices = vertices;
    def.vertexCount = 3;
    def.indices = indices;
    def.triangleCount = 1;
    def.weldVertices = weld;
    def.weldTolerance = 0.005f;
    int baseline = b3GetByteCount();
    for (int i = 0; i < 1000; ++i)
    {
        b3MeshData* mesh = b3CreateMesh(&def, NULL, 0);
        if (mesh != NULL)
        {
            b3DestroyMesh(mesh);
            fprintf(stderr, "Expected mesh creation to fail\n");
            return 1;
        }
    }
    int leaked = b3GetByteCount() - baseline;
    printf("weld=%d, failed creations=1000, leaked bytes=%d\n", weld, leaked);
    return leaked != 0;
}

int main(void)
{
    const b3Vec3 degenerate[] = {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}};
    const b3Vec3 collapsed[] = {{0, 0, 0}, {0.001f, 0, 0}, {0, 0.001f, 0}};
    const b3Vec3 outOfBounds[] = {{2 * B3_HUGE, 0, 0}, {2 * B3_HUGE, 1, 0}, {2 * B3_HUGE, 0, 1}};
    return CheckFailure(degenerate, false) | CheckFailure(collapsed, true) | CheckFailure(outOfBounds, false);
}
