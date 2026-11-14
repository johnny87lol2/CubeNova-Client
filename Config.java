package net.cubenova.client;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import net.fabricmc.loader.api.FabricLoader;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/** Persistent settings (config/cubenova.json). */
public final class Config {
    private Config() {}

    public static final class ModuleState {
        public boolean enabled;
        public int x, y;
        public ModuleState() {}
        public ModuleState(boolean enabled, int x, int y) { this.enabled = enabled; this.x = x; this.y = y; }
    }

    private static final class Data {
        Map<String, ModuleState> modules = new LinkedHashMap<>();
        int crosshairColor = 0xFF1BD96A;
        int outlineColor = 0xFF1BD96A;
    }

    /** Colours the menu button cycles through. */
    public static final int[] PALETTE = {
        0xFF1BD96A, 0xFFFFFFFF, 0xFFFF4D4D, 0xFF4DA6FF, 0xFFFFD84D, 0xFFDB8CFF, 0xFFFF9F43
    };

    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final Path FILE = FabricLoader.getInstance().getConfigDir().resolve("cubenova.json");
    private static Data data = new Data();

    public static void load() {
        try {
            if (Files.exists(FILE)) {
                Data d = GSON.fromJson(Files.readString(FILE), Data.class);
                if (d != null) data = d;
            }
        } catch (Exception ignored) {}
        if (data.modules == null) data.modules = new LinkedHashMap<>();
    }

    public static void save() {
        try {
            Files.writeString(FILE, GSON.toJson(data));
        } catch (IOException ignored) {}
    }

    /** Get (or create with defaults) the state of a module. */
    public static ModuleState module(String id, boolean defEnabled, int defX, int defY) {
        return data.modules.computeIfAbsent(id, k -> new ModuleState(defEnabled, defX, defY));
    }

    public static boolean enabled(String id) {
        ModuleState s = data.modules.get(id);
        return s != null && s.enabled;
    }

    public static int crosshairColor() { return data.crosshairColor; }
    public static int outlineColor() { return data.outlineColor; }

    public static int cycleCrosshairColor() {
        data.crosshairColor = next(data.crosshairColor);
        return data.crosshairColor;
    }

    public static int cycleOutlineColor() {
        data.outlineColor = next(data.outlineColor);
        return data.outlineColor;
    }

    private static int next(int cur) {
        for (int i = 0; i < PALETTE.length; i++) if (PALETTE[i] == cur) return PALETTE[(i + 1) % PALETTE.length];
        return PALETTE[0];
    }

    public static List<String> ids() { return new ArrayList<>(data.modules.keySet()); }
}
