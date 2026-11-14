package net.cubenova.client.feature;

import net.cubenova.client.Config;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.option.Perspective;

/** Look around the player without turning them. */
public final class FreeLook {
    private FreeLook() {}

    public static boolean active;
    public static float yaw, pitch;
    private static Perspective previous = Perspective.FIRST_PERSON;

    public static void update(MinecraftClient mc, boolean held) {
        boolean want = held && Config.enabled("freelook") && mc.player != null && mc.currentScreen == null;
        if (want && !active) {
            active = true;
            yaw = mc.player.getYaw();
            pitch = mc.player.getPitch();
            previous = mc.options.getPerspective();
            mc.options.setPerspective(Perspective.THIRD_PERSON_BACK);
        } else if (!want && active) {
            active = false;
            mc.options.setPerspective(previous);
        }
    }
}
