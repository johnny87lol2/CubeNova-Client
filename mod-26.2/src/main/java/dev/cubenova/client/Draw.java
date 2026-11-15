package dev.cubenova.client;

import net.minecraft.client.gui.GuiGraphicsExtractor;

/** 小型繪圖工具：圓角矩形、透明度 */
public final class Draw {
    private Draw() {}

    public static final int ACCENT = 0xFF1BD96A;

    /** 把 ARGB 顏色的透明度乘上 f (0..1) */
    public static int alpha(int argb, float f) {
        f = Math.max(0f, Math.min(1f, f));
        int a = (int) (((argb >>> 24) & 0xFF) * f);
        return (a << 24) | (argb & 0xFFFFFF);
    }

    /** 兩色線性混合 */
    public static int mix(int c1, int c2, float t) {
        t = Math.max(0f, Math.min(1f, t));
        int a = (int) (((c1 >>> 24) & 0xFF) * (1 - t) + ((c2 >>> 24) & 0xFF) * t);
        int r = (int) (((c1 >> 16) & 0xFF) * (1 - t) + ((c2 >> 16) & 0xFF) * t);
        int g = (int) (((c1 >> 8) & 0xFF) * (1 - t) + ((c2 >> 8) & 0xFF) * t);
        int b = (int) ((c1 & 0xFF) * (1 - t) + (c2 & 0xFF) * t);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    public static void roundRect(GuiGraphicsExtractor g, int x, int y, int w, int h, int r, int color) {
        r = Math.min(r, Math.min(w, h) / 2);
        if (r <= 0) {
            g.fill(x, y, x + w, y + h, color);
            return;
        }
        for (int i = 0; i < r; i++) {
            double dy = r - i - 0.5;
            int inset = (int) Math.round(r - Math.sqrt(Math.max(0, r * r - dy * dy)));
            g.fill(x + inset, y + i, x + w - inset, y + i + 1, color);
            g.fill(x + inset, y + h - i - 1, x + w - inset, y + h - i, color);
        }
        g.fill(x, y + r, x + w, y + h - r, color);
    }

    /** 帶 1px 邊框的圓角面板 */
    public static void panel(GuiGraphicsExtractor g, int x, int y, int w, int h, int r, int border, int fill) {
        roundRect(g, x, y, w, h, r, border);
        roundRect(g, x + 1, y + 1, w - 2, h - 2, Math.max(0, r - 1), fill);
    }

    /** 依寬度截斷字串 */
    public static String fit(net.minecraft.client.gui.Font font, String s, int maxW) {
        if (font.width(s) <= maxW) return s;
        while (s.length() > 1 && font.width(s + "..") > maxW) s = s.substring(0, s.length() - 1);
        return s + "..";
    }

    /** 以 scale 倍數繪製文字 (x,y 為縮放後的左上角座標) */
    public static void text(GuiGraphicsExtractor g, net.minecraft.client.gui.Font font, net.minecraft.network.chat.Component c,
                            int x, int y, int color, float scale) {
        g.pose().pushMatrix();
        g.pose().scale(scale, scale);
        g.text(font, c, Math.round(x / scale), Math.round(y / scale), color, false);
        g.pose().popMatrix();
    }
}
