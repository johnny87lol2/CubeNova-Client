package net.cubenova.client.feature;

import net.cubenova.client.Config;

/** FOV multiplier applied by GameRendererMixin. */
public final class Zoom {
    private Zoom() {}

    private static boolean active;
    private static double current = 1.0;

    public static void setActive(boolean on) { active = on && Config.enabled("zoom"); }

    /** Smoothly move toward the target; called every client tick. */
    public static void tick() {
        double target = active ? 0.25 : 1.0;
        current += (target - current) * 0.35;
        if (Math.abs(target - current) < 0.001) current = target;
    }

    public static double factor() { return current; }
}
