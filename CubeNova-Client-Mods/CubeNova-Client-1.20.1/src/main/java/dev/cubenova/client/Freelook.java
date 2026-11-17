package dev.cubenova.client;

import net.minecraft.client.CameraType;
import net.minecraft.client.Minecraft;
import net.minecraft.util.Mth;

/** Freelook: hold the key to rotate the camera without turning the player. */
public final class Freelook {
    private Freelook() {}

    public static volatile boolean active = false;
    public static float yaw, pitch;
    private static CameraType saved = null;

    public static void tick(Minecraft mc) {
        boolean want = HudConfig.get().on("freelook") && CubeNovaClient.freelookKey != null
                && CubeNovaClient.freelookKey.isDown() && mc.player != null && mc.screen == null;
        if (want && !active) {
            yaw = mc.player.getYRot();
            pitch = mc.player.getXRot();
            saved = mc.options.getCameraType();
            if (saved == CameraType.FIRST_PERSON) mc.options.setCameraType(CameraType.THIRD_PERSON_BACK);
            active = true;
        } else if (!want && active) {
            active = false;
            if (saved != null && mc.options.getCameraType() != saved) mc.options.setCameraType(saved);
            saved = null;
        }
    }

    /** Called instead of Entity.turn while freelook is active (same 0.15 factor as vanilla). */
    public static void turn(double dYaw, double dPitch) {
        yaw += (float) dYaw * 0.15f;
        pitch = Mth.clamp(pitch + (float) dPitch * 0.15f, -90f, 90f);
    }
}
