package net.cubenova.client.hud;

import net.cubenova.client.Config;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;

/** A draggable on-screen element. */
public abstract class HudElement {
    public final String id;
    public final String title;
    private final Config.ModuleState state;

    protected HudElement(String id, String title, int defX, int defY) {
        this.id = id;
        this.title = title;
        this.state = Config.module(id, true, defX, defY);
    }

    public Config.ModuleState state() { return state; }
    public boolean enabled() { return state.enabled; }
    public int x() { return state.x; }
    public int y() { return state.y; }
    public abstract int width();
    public abstract int height();
    /** Draw with the top-left corner at (x, y). */
    public abstract void draw(DrawContext ctx, MinecraftClient mc, int x, int y);

    /** Keep the element on screen. */
    public void clamp(int screenW, int screenH) {
        state.x = Math.max(0, Math.min(state.x, Math.max(0, screenW - width())));
        state.y = Math.max(0, Math.min(state.y, Math.max(0, screenH - height())));
    }

    public boolean hit(double mx, double my) {
        return mx >= x() && mx <= x() + width() && my >= y() && my <= y() + height();
    }
}
