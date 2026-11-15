package net.cubenova.client.feature;

import net.cubenova.client.Config;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderContext;
import net.minecraft.client.render.VertexConsumer;
import net.minecraft.client.util.math.MatrixStack;
import net.minecraft.block.ShapeContext;
import net.minecraft.util.math.BlockPos;
import net.minecraft.util.math.MathHelper;
import net.minecraft.util.shape.VoxelShape;

/**
 * Custom coloured block outline. If this file ever fails to compile on a different
 * Minecraft version, delete it together with its registration in CubeNovaClient.
 */
public final class BlockOutline {
    private BlockOutline() {}

    /** @return true to let vanilla draw the outline too, false to replace it. */
    public static boolean render(WorldRenderContext ctx, WorldRenderContext.BlockOutlineContext oc) {
        if (!Config.enabled("blockoutline")) return true;
        BlockPos pos = oc.blockPos();
        VoxelShape shape = oc.blockState().getOutlineShape(ctx.world(), pos, ShapeContext.of(oc.entity()));
        VertexConsumer vc = oc.vertexConsumer();
        MatrixStack.Entry entry = ctx.matrixStack().peek();

        int argb = Config.outlineColor();
        float r = ((argb >> 16) & 0xFF) / 255f, g = ((argb >> 8) & 0xFF) / 255f, b = (argb & 0xFF) / 255f;
        double ox = pos.getX() - oc.cameraX(), oy = pos.getY() - oc.cameraY(), oz = pos.getZ() - oc.cameraZ();

        shape.forEachEdge((x1, y1, z1, x2, y2, z2) -> {
            float nx = (float) (x2 - x1), ny = (float) (y2 - y1), nz = (float) (z2 - z1);
            float len = MathHelper.sqrt(nx * nx + ny * ny + nz * nz);
            if (len == 0) return;
            nx /= len; ny /= len; nz /= len;
            vc.vertex(entry, (float) (x1 + ox), (float) (y1 + oy), (float) (z1 + oz)).color(r, g, b, 0.9f).normal(entry, nx, ny, nz);
            vc.vertex(entry, (float) (x2 + ox), (float) (y2 + oy), (float) (z2 + oz)).color(r, g, b, 0.9f).normal(entry, nx, ny, nz);
        });
        return false;
    }
}
