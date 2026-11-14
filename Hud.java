package net.cubenova.client.hud;

import net.cubenova.client.Config;
import net.cubenova.client.feature.Cps;
import net.cubenova.client.feature.Stats;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.render.RenderTickCounter;

import java.util.ArrayList;
import java.util.List;

public final class Hud {
    private Hud() {}

    public static final List<HudElement> ELEMENTS = new ArrayList<>();

    public static void init() {
        ELEMENTS.add(new TextElement("fps", "FPS", 6, 6, 1,
            () -> MinecraftClient.getInstance().getCurrentFps() + " FPS"));
        ELEMENTS.add(new TextElement("cps", "CPS", 6, 26, 1,
            () -> Cps.left() + " | " + Cps.right() + " CPS"));
        ELEMENTS.add(new TextElement("coords", "Coordinates", 6, 46, 3, () -> {
            var p = MinecraftClient.getInstance().player;
            if (p == null) return "X: -\nY: -\nZ: -";
            return String.format("X: %.1f\nY: %.1f\nZ: %.1f", p.getX(), p.getY(), p.getZ());
        }));
        ELEMENTS.add(new TextElement("combo", "Combo", 6, 90, 1, () -> "Combo: " + Stats.combo()));
        ELEMENTS.add(new TextElement("reach", "Reach", 6, 110, 1, () -> "Reach: " + Stats.reachText()));
        ELEMENTS.add(new KeystrokesElement(6, 134));
    }

    public static void render(DrawContext ctx, RenderTickCounter tick) {
        MinecraftClient mc = MinecraftClient.getInstance();
        Cps.poll(mc);
        if (mc.player == null || mc.options.hudHidden) return;
        int w = ctx.getScaledWindowWidth(), h = ctx.getScaledWindowHeight();
        for (HudElement e : ELEMENTS) {
            if (!e.enabled()) continue;
            e.clamp(w, h);
            e.draw(ctx, mc, e.x(), e.y());
        }
        if (Config.enabled("crosshair")) drawCrosshair(ctx, w / 2, h / 2);
    }

    /** Simple custom crosshair (the vanilla one is hidden by InGameHudMixin). */
    private static void drawCrosshair(DrawContext ctx, int cx, int cy) {
        int c = Config.crosshairColor();
        int len = 5, gap = 2, t = 1;
        ctx.fill(cx - gap - len, cy - t, cx - gap, cy + t + 1, c);
        ctx.fill(cx + gap + 1, cy - t, cx + gap + len + 1, cy + t + 1, c);
        ctx.fill(cx - t, cy - gap - len, cx + t + 1, cy - gap, c);
        ctx.fill(cx - t, cy + gap + 1, cx + t + 1, cy + gap + len + 1, c);
    }
}
