package net.cubenova.client.hud;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;

import java.util.function.Supplier;

/** A small "label: value" box (FPS, CPS, coords, combo, reach). */
public class TextElement extends HudElement {
    private final Supplier<String> text;
    private final int lines;

    public TextElement(String id, String title, int defX, int defY, int lines, Supplier<String> text) {
        super(id, title, defX, defY);
        this.text = text;
        this.lines = lines;
    }

    @Override public int width() {
        MinecraftClient mc = MinecraftClient.getInstance();
        int w = 40;
        for (String l : text.get().split("\n")) w = Math.max(w, mc.textRenderer.getWidth(l) + 10);
        return w;
    }

    @Override public int height() { return 6 + lines * 10; }

    @Override public void draw(DrawContext ctx, MinecraftClient mc, int x, int y) {
        ctx.fill(x, y, x + width(), y + height(), 0x90000000);
        ctx.fill(x, y, x + 2, y + height(), 0xFF1BD96A);
        int ly = y + 4;
        for (String l : text.get().split("\n")) {
            ctx.drawTextWithShadow(mc.textRenderer, l, x + 6, ly, 0xFFFFFFFF);
            ly += 10;
        }
    }
}
