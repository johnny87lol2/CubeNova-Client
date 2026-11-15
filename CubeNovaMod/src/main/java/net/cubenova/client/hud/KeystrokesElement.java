package net.cubenova.client.hud;

import net.cubenova.client.feature.Cps;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.option.KeyBinding;

/** WASD + mouse buttons + space, like Lunar's keystrokes. */
public class KeystrokesElement extends HudElement {
    private static final int K = 22, GAP = 2;

    public KeystrokesElement(int defX, int defY) { super("keystrokes", "Keystrokes", defX, defY); }

    @Override public int width() { return K * 3 + GAP * 2; }
    @Override public int height() { return K * 4 + GAP * 3 - 8; }

    private void key(DrawContext ctx, MinecraftClient mc, int x, int y, int w, int h, String label, boolean down) {
        ctx.fill(x, y, x + w, y + h, down ? 0xB0FFFFFF : 0x90000000);
        int tw = mc.textRenderer.getWidth(label);
        ctx.drawText(mc.textRenderer, label, x + (w - tw) / 2, y + (h - 8) / 2, down ? 0xFF000000 : 0xFFFFFFFF, false);
    }

    @Override public void draw(DrawContext ctx, MinecraftClient mc, int x, int y) {
        var o = mc.options;
        KeyBinding[] k = { o.forwardKey, o.leftKey, o.backKey, o.rightKey, o.jumpKey };
        key(ctx, mc, x + K + GAP, y, K, K, "W", k[0].isPressed());
        key(ctx, mc, x, y + K + GAP, K, K, "A", k[1].isPressed());
        key(ctx, mc, x + K + GAP, y + K + GAP, K, K, "S", k[2].isPressed());
        key(ctx, mc, x + (K + GAP) * 2, y + K + GAP, K, K, "D", k[3].isPressed());
        int half = (K * 3 + GAP * 2 - GAP) / 2;
        key(ctx, mc, x, y + (K + GAP) * 2, half, K, "LMB " + Cps.left(), o.attackKey.isPressed());
        key(ctx, mc, x + half + GAP, y + (K + GAP) * 2, half, K, "RMB " + Cps.right(), o.useKey.isPressed());
        key(ctx, mc, x, y + (K + GAP) * 3, K * 3 + GAP * 2, 14, "SPACE", k[4].isPressed());
    }
}
