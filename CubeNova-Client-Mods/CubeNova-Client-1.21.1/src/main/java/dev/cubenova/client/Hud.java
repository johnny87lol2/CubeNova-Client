package dev.cubenova.client;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.world.effect.MobEffectInstance;
import net.minecraft.world.effect.MobEffects;
import org.lwjgl.glfw.GLFW;

import java.time.LocalTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayDeque;
import java.util.Deque;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.Map;

/** HUD 元件、CPS / FPS 計數、切換衝刺與縮放 */
public final class Hud {
    private Hud() {}

    // ───────────── 元件 ─────────────
    public abstract static class Element {
        public final String id;
        public final boolean defaultOn;
        public final int defaultX;
        public final int defaultY;

        Element(String id, boolean defaultOn, int x, int y) {
            this.id = id;
            this.defaultOn = defaultOn;
            this.defaultX = x;
            this.defaultY = y;
        }

        abstract int width(Font font);
        abstract int height(Font font);
        abstract void draw(GuiGraphics g, Font font, int x, int y);
    }

    /** 單行文字的方塊 */
    static final class TextBox extends Element {
        private final java.util.function.Supplier<String> text;

        TextBox(String id, boolean on, int x, int y, java.util.function.Supplier<String> text) {
            super(id, on, x, y);
            this.text = text;
        }

        @Override int width(Font font) { return font.width(text.get()) + 16; }
        @Override int height(Font font) { return 16; }

        @Override
        void draw(GuiGraphics g, Font font, int x, int y) {
            Draw.roundRect(g, x, y, width(font), 16, 5, 0xB0101420);
            Draw.roundRect(g, x + 4, y + 4, 2, 8, 1, Draw.ACCENT);
            g.drawString(font, text.get(), x + 10, y + 4, 0xFFFFFFFF, true);
        }
    }

    /** WASD / 空白 / 滑鼠鍵 */
    static final class Keystrokes extends Element {
        private static final int S = 20;
        private static final int GAP = 2;

        Keystrokes(int x, int y) { super("keystrokes", true, x, y); }

        @Override int width(Font font) { return S * 3 + GAP * 2; }
        @Override int height(Font font) { return S * 3 + GAP * 3 + 2; }

        private void key(GuiGraphics g, Font font, int x, int y, int w, int h, String label, boolean down) {
            Draw.roundRect(g, x, y, w, h, 5, down ? 0xE01BD96A : 0xB0101420);
            int color = down ? 0xFF04140A : 0xFFFFFFFF;
            g.drawString(font, label, x + (w - font.width(label)) / 2, y + (h - 8) / 2, color, false);
        }

        @Override
        void draw(GuiGraphics g, Font font, int x, int y) {
            Minecraft mc = Minecraft.getInstance();
            var o = mc.options;
            int col = S + GAP;
            key(g, font, x + col, y, S, S, "W", o.keyUp.isDown());
            key(g, font, x, y + col, S, S, "A", o.keyLeft.isDown());
            key(g, font, x + col, y + col, S, S, "S", o.keyDown.isDown());
            key(g, font, x + col * 2, y + col, S, S, "D", o.keyRight.isDown());
            int w2 = (S * 3 + GAP * 2 - GAP) / 2;
            key(g, font, x, y + col * 2, w2, S, "LMB", o.keyAttack.isDown());
            key(g, font, x + w2 + GAP, y + col * 2, w2, S, "RMB", o.keyUse.isDown());
            Draw.roundRect(g, x, y + col * 3, S * 3 + GAP * 2, 5, 2, o.keyJump.isDown() ? 0xE01BD96A : 0xB0101420);
        }
    }

    public static final Map<String, Element> ELEMENTS = new LinkedHashMap<>();

    static {
        add(new TextBox("fps", true, 6, 6, () -> Hud.fps + " FPS"));
        add(new TextBox("cps", true, 6, 26, () -> cps(0) + " | " + cps(1) + " CPS"));
        add(new TextBox("coords", true, 6, 46, Hud::coords));
        add(new TextBox("direction", false, 6, 66, Hud::direction));
        add(new TextBox("clock", false, 6, 86, () -> LocalTime.now().format(DateTimeFormatter.ofPattern("HH:mm:ss"))));
        add(new Keystrokes(6, 108));
    }

    private static void add(Element e) { ELEMENTS.put(e.id, e); }

    // ───────────── 資料 ─────────────
    private static int fps = 0;
    private static int frames = 0;
    private static long lastFpsTime = System.nanoTime();

    private static final Deque<Long> LEFT = new ArrayDeque<>();
    private static final Deque<Long> RIGHT = new ArrayDeque<>();
    private static boolean leftWas, rightWas;

    private static int cps(int button) {
        Deque<Long> q = button == 0 ? LEFT : RIGHT;
        long now = System.currentTimeMillis();
        while (!q.isEmpty() && now - q.peekFirst() > 1000) q.pollFirst();
        return q.size();
    }

    private static String coords() {
        Minecraft mc = Minecraft.getInstance();
        if (mc.player == null) return "XYZ";
        return String.format(Locale.ROOT, "XYZ: %.1f / %.1f / %.1f", mc.player.getX(), mc.player.getY(), mc.player.getZ());
    }

    private static String direction() {
        Minecraft mc = Minecraft.getInstance();
        if (mc.player == null) return "";
        String n = mc.player.getDirection().getName();
        return n.substring(0, 1).toUpperCase(Locale.ROOT) + n.substring(1);
    }

    // ───────────── 繪製 ─────────────
    public static void render(GuiGraphics g) {
        Minecraft mc = Minecraft.getInstance();
        if (mc.options.hideGui || mc.screen instanceof HudScreen) return;
        countFrame();
        Font font = mc.font;
        HudConfig cfg = HudConfig.get();
        Draw.pushScale(g, Draw.hudScale());
        for (Element e : ELEMENTS.values()) {
            if (!cfg.on(e.id)) continue;
            HudConfig.ModCfg c = cfg.of(e.id);
            e.draw(g, font, c.x, c.y);
        }
        Draw.popScale(g);
    }

    /** 編輯畫面使用：全部畫出來(含被關閉的，變暗) */
    public static void renderEditor(GuiGraphics g, Font font) {
        Draw.pushScale(g, Draw.hudScale());
        try {
            editorInner(g, font);
        } finally {
            Draw.popScale(g);
        }
    }

    private static void editorInner(GuiGraphics g, Font font) {
        countFrame();
        HudConfig cfg = HudConfig.get();
        for (Element e : ELEMENTS.values()) {
            HudConfig.ModCfg c = cfg.of(e.id);
            e.draw(g, font, c.x, c.y);
            int w = e.width(font), h = e.height(font);
            int col = cfg.on(e.id) ? 0xFF1BD96A : 0xFF666666;
            g.fill(c.x - 1, c.y - 1, c.x + w + 1, c.y, col);
            g.fill(c.x - 1, c.y + h, c.x + w + 1, c.y + h + 1, col);
            g.fill(c.x - 1, c.y, c.x, c.y + h, col);
            g.fill(c.x + w, c.y, c.x + w + 1, c.y + h, col);
        }
    }

    private static void countFrame() {
        frames++;
        long now = System.nanoTime();
        if (now - lastFpsTime >= 1_000_000_000L) {
            fps = frames;
            frames = 0;
            lastFpsTime = now;
        }
    }

    // ───────────── 每 tick ─────────────
    private static boolean zooming = false;
    private static boolean nvApplied = false;
    private static int savedFov = 70;

    public static void tick(Minecraft mc) {
        // CPS：用 GLFW 輪詢滑鼠左右鍵的按下邊緣
        if (mc.getWindow() != null) {
            long win = mc.getWindow().getWindow();
            boolean l = GLFW.glfwGetMouseButton(win, GLFW.GLFW_MOUSE_BUTTON_LEFT) == GLFW.GLFW_PRESS;
            boolean r = GLFW.glfwGetMouseButton(win, GLFW.GLFW_MOUSE_BUTTON_RIGHT) == GLFW.GLFW_PRESS;
            long now = System.currentTimeMillis();
            if (l && !leftWas && mc.screen == null) LEFT.addLast(now);
            if (r && !rightWas && mc.screen == null) RIGHT.addLast(now);
            leftWas = l;
            rightWas = r;
        }

        HudConfig cfg = HudConfig.get();

        // 切換衝刺：按住前進時自動衝刺
        if (cfg.on("sprint") && mc.player != null && mc.screen == null
                && mc.options.keyUp.isDown() && !mc.player.isShiftKeyDown()) {
            mc.options.keySprint.setDown(true);
        }

        // 夜視：在本機加上夜視效果 (不會影響伺服器)
        if (mc.player != null) {
            if (cfg.on("nightvision")) {
                MobEffectInstance cur = mc.player.getEffect(MobEffects.NIGHT_VISION);
                if (cur == null || cur.getDuration() < 400) {
                    mc.player.addEffect(new MobEffectInstance(MobEffects.NIGHT_VISION, 100000, 0, false, false, false));
                    nvApplied = true;
                }
            } else if (nvApplied) {
                mc.player.removeEffect(MobEffects.NIGHT_VISION);
                nvApplied = false;
            }
        } else {
            nvApplied = false;
        }

        // 縮放：按住按鍵時把 FOV 降到 30，放開後還原
        boolean want = cfg.on("zoom") && mc.screen == null && CubeNovaClient.zoomKey != null && CubeNovaClient.zoomKey.isDown();
        if (want && !zooming) {
            savedFov = mc.options.fov().get();
            zooming = true;
            mc.options.fov().set(30);
        } else if (!want && zooming) {
            zooming = false;
            mc.options.fov().set(savedFov);
        }
    }
}
