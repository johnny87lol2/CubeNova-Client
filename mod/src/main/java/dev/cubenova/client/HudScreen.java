package dev.cubenova.client;

import net.minecraft.ChatFormatting;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

import java.util.Map;

/** 右 Shift 開啟的現代風格選單：自繪的卡片 + 動態開關，並可拖曳 HUD。 */
public class HudScreen extends Screen {
    private record Entry(String id, String name, String desc) {}

    private static final Entry[] ENTRIES = {
        new Entry("fps", "FPS", "Frames per second"),
        new Entry("cps", "CPS", "Clicks per second"),
        new Entry("keystrokes", "Keystrokes", "WASD and mouse"),
        new Entry("coords", "Coordinates", "XYZ position"),
        new Entry("direction", "Direction", "Facing direction"),
        new Entry("clock", "Clock", "Real time"),
        new Entry("nightvision", "Night Vision", "See in the dark"),
        new Entry("sprint", "Toggle Sprint", "Auto-sprint forward"),
        new Entry("zoom", "Zoom", "Hold C to zoom"),
        new Entry("freelook", "Freelook", "Hold Left Alt to look around"),
        new Entry("capes", "Custom Capes", "CubeNova player capes"),
        new Entry("uncap", "Max Performance", "No FPS cap, VSync off"),
    };

    private static final int CW = 196, CH = 34, GAP = 8, PAD = 16, HEADER = 58, FOOT = 46;
    private static final int BG_DIM = 0xB0070910;
    private static final int PANEL_FILL = 0xF2111620;
    private static final int PANEL_BORDER = 0xFF262D3B;
    private static final int CELL = 0xFF161B26;
    private static final int CELL_HOVER = 0xFF1C2330;
    private static final int TX = 0xFFEEF1F6;
    private static final int TX_MUTED = 0xFF7C8596;

    private final float[] anim = new float[ENTRIES.length];
    private final long opened = System.currentTimeMillis();
    private String dragging = null;
    private int dragDx, dragDy;
    private int px, py, pw, ph;

    public HudScreen() {
        super(Component.literal("CubeNova Client"));
        for (int i = 0; i < ENTRIES.length; i++) anim[i] = HudConfig.get().on(ENTRIES[i].id()) ? 1f : 0f;
    }

    @Override
    protected void init() {
        layout();
    }

    private void layout() {
        int rows = (ENTRIES.length + 1) / 2;
        pw = PAD * 2 + CW * 2 + GAP;
        ph = HEADER + rows * CH + (rows - 1) * GAP + FOOT;
        px = (this.width - pw) / 2;
        py = (this.height - ph) / 2;
    }

    private int cellX(int i) { return px + PAD + (i % 2) * (CW + GAP); }
    private int cellY(int i) { return py + HEADER + (i / 2) * (CH + GAP); }
    private int doneX() { return px + pw / 2 - 52; }
    private int doneY() { return py + ph - FOOT + 12; }

    private static boolean in(double mx, double my, int x, int y, int w, int h) {
        return mx >= x && mx < x + w && my >= y && my < y + h;
    }

    @Override
    public void render(GuiGraphics g, int mouseX, int mouseY, float partialTick) {
        layout();
        float f = Math.min(1f, (System.currentTimeMillis() - opened) / 180f);
        f = 1f - (1f - f) * (1f - f); // ease-out

        g.fill(0, 0, this.width, this.height, Draw.alpha(BG_DIM, f));
        Hud.renderEditor(g, this.font);

        // 陰影 + 面板
        Draw.roundRect(g, px - 3, py - 3, pw + 6, ph + 6, 14, Draw.alpha(0x50000000, f));
        Draw.panel(g, px, py, pw, ph, 12, Draw.alpha(PANEL_BORDER, f), Draw.alpha(PANEL_FILL, f));

        // 標題
        Component brand = Component.literal("CubeNova").withStyle(ChatFormatting.BOLD);
        Component sub = Component.literal(" Client").withStyle(ChatFormatting.BOLD);
        Draw.text(g, this.font, brand, px + PAD, py + 14, Draw.alpha(Draw.ACCENT, f), 1.5f);
        int bw = (int) (this.font.width(brand) * 1.5f);
        Draw.text(g, this.font, sub, px + PAD + bw, py + 14, Draw.alpha(TX, f), 1.5f);
        g.drawString(this.font, "Right Shift to close  -  drag HUD elements to move them",
                px + PAD, py + 36, Draw.alpha(TX_MUTED, f), false);
        g.fill(px + PAD, py + HEADER - 6, px + pw - PAD, py + HEADER - 5, Draw.alpha(PANEL_BORDER, f));

        // 模組卡片
        for (int i = 0; i < ENTRIES.length; i++) {
            Entry e = ENTRIES[i];
            int x = cellX(i), y = cellY(i);
            boolean on = HudConfig.get().on(e.id());
            anim[i] += ((on ? 1f : 0f) - anim[i]) * 0.3f;
            boolean hover = in(mouseX, mouseY, x, y, CW, CH);

            int border = Draw.mix(0xFF232A38, Draw.alpha(Draw.ACCENT, 0.55f), anim[i]);
            Draw.panel(g, x, y, CW, CH, 8, Draw.alpha(border, f), Draw.alpha(hover ? CELL_HOVER : CELL, f));
            g.drawString(this.font, Component.literal(e.name()).withStyle(ChatFormatting.BOLD),
                    x + 11, y + 7, Draw.alpha(TX, f), false);
            String desc = this.font.plainSubstrByWidth(e.desc(), CW - 70);
            g.drawString(this.font, desc, x + 11, y + 19, Draw.alpha(TX_MUTED, f), false);

            // 開關
            int sw = 30, sh = 14;
            int sx = x + CW - sw - 11, sy = y + (CH - sh) / 2;
            int track = Draw.mix(0xFF2B3342, Draw.ACCENT, anim[i]);
            Draw.roundRect(g, sx, sy, sw, sh, 7, Draw.alpha(track, f));
            int kx = sx + 2 + Math.round((sw - sh) * anim[i]);
            Draw.roundRect(g, kx, sy + 2, 10, 10, 5, Draw.alpha(Draw.mix(0xFFB6BECC, 0xFF06150C, anim[i]), f));
        }

        // Done 按鈕
        boolean dh = in(mouseX, mouseY, doneX(), doneY(), 104, 22);
        Draw.roundRect(g, doneX(), doneY(), 104, 22, 8, Draw.alpha(dh ? 0xFF3CEB86 : Draw.ACCENT, f));
        Component done = Component.literal("Done").withStyle(ChatFormatting.BOLD);
        g.drawString(this.font, done, doneX() + (104 - this.font.width(done)) / 2, doneY() + 7,
                Draw.alpha(0xFF06150C, f), false);
    }

    @Override
    public boolean mouseClicked(double mx, double my, int button) {
        if (button != 0) return false;
        layout();

        for (int i = 0; i < ENTRIES.length; i++) {
            if (in(mx, my, cellX(i), cellY(i), CW, CH)) {
                HudConfig.ModCfg c = HudConfig.get().of(ENTRIES[i].id());
                c.enabled = !c.enabled;
                if (ENTRIES[i].id().equals("uncap") && c.enabled) CubeNovaClient.applyPerformance(this.minecraft);
                if (ENTRIES[i].id().equals("zoom") && c.enabled) CubeNovaClient.freeZoomKey(this.minecraft);
                HudConfig.save();
                return true;
            }
        }
        if (in(mx, my, doneX(), doneY(), 104, 22)) {
            this.onClose();
            return true;
        }
        if (in(mx, my, px, py, pw, ph)) return true; // 面板內空白處不穿透

        String hit = null;
        for (Map.Entry<String, Hud.Element> e : Hud.ELEMENTS.entrySet()) {
            HudConfig.ModCfg c = HudConfig.get().of(e.getKey());
            if (in(mx, my, c.x, c.y, e.getValue().width(this.font), e.getValue().height(this.font))) hit = e.getKey();
        }
        if (hit != null) {
            HudConfig.ModCfg c = HudConfig.get().of(hit);
            dragging = hit;
            dragDx = (int) mx - c.x;
            dragDy = (int) my - c.y;
            return true;
        }
        return false;
    }

    @Override
    public boolean mouseDragged(double mx, double my, int button, double dx, double dy) {
        if (dragging != null) {
            Hud.Element e = Hud.ELEMENTS.get(dragging);
            HudConfig.ModCfg c = HudConfig.get().of(dragging);
            c.x = Math.max(0, Math.min(this.width - e.width(this.font), (int) mx - dragDx));
            c.y = Math.max(0, Math.min(this.height - e.height(this.font), (int) my - dragDy));
            return true;
        }
        return super.mouseDragged(mx, my, button, dx, dy);
    }

    @Override
    public boolean mouseReleased(double mx, double my, int button) {
        if (dragging != null) {
            dragging = null;
            HudConfig.save();
            return true;
        }
        return super.mouseReleased(mx, my, button);
    }

    @Override
    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (CubeNovaClient.menuKey != null && CubeNovaClient.menuKey.matches(keyCode, scanCode)) {
            this.onClose();
            return true;
        }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }

    @Override
    public void onClose() {
        HudConfig.save();
        super.onClose();
    }
}
