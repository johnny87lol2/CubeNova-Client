package net.cubenova.client.feature;

import net.minecraft.client.network.ClientPlayerEntity;
import net.minecraft.entity.Entity;
import net.minecraft.entity.player.PlayerEntity;
import net.minecraft.util.math.Box;
import net.minecraft.util.math.MathHelper;
import net.minecraft.util.math.Vec3d;

/** Combo counter and last-hit reach. */
public final class Stats {
    private Stats() {}

    private static int combo;
    private static double reach = -1;
    private static int lastHurt;

    public static int combo() { return combo; }

    public static String reachText() {
        return reach < 0 ? "-" : String.format("%.2f", reach);
    }

    public static void onAttack(PlayerEntity player, Entity target) {
        combo++;
        Vec3d eye = player.getEyePos();
        Box b = target.getBoundingBox();
        double dx = MathHelper.clamp(eye.x, b.minX, b.maxX) - eye.x;
        double dy = MathHelper.clamp(eye.y, b.minY, b.maxY) - eye.y;
        double dz = MathHelper.clamp(eye.z, b.minZ, b.maxZ) - eye.z;
        reach = Math.sqrt(dx * dx + dy * dy + dz * dz);
    }

    /** Called every client tick: getting hit resets the combo. */
    public static void tick(ClientPlayerEntity p) {
        if (p == null) { combo = 0; lastHurt = 0; return; }
        if (p.hurtTime > 0 && lastHurt == 0) combo = 0;
        lastHurt = p.hurtTime;
    }
}
