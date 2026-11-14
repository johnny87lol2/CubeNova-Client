package dev.cubenova.client;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import net.fabricmc.loader.api.FabricLoader;

import java.io.IOException;
import java.io.Reader;
import java.io.Writer;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Map;

/** 設定檔: <遊戲資料夾>/config/cubenova.json (啟動器也會改 enabled 欄位) */
public final class HudConfig {
    public static final class ModCfg {
        public boolean enabled;
        public int x;
        public int y;
        ModCfg(boolean enabled, int x, int y) { this.enabled = enabled; this.x = x; this.y = y; }
    }

    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final Path FILE = FabricLoader.getInstance().getConfigDir().resolve("cubenova.json");

    public Map<String, ModCfg> modules = new LinkedHashMap<>();

    private static HudConfig instance = new HudConfig();

    public static HudConfig get() { return instance; }

    public static void load() {
        HudConfig cfg = null;
        if (Files.exists(FILE)) {
            try (Reader r = Files.newBufferedReader(FILE)) {
                cfg = GSON.fromJson(r, HudConfig.class);
            } catch (Exception e) {
                CubeNovaClient.LOGGER.warn("Could not read cubenova.json, using defaults", e);
            }
        }
        if (cfg == null) cfg = new HudConfig();
        if (cfg.modules == null) cfg.modules = new LinkedHashMap<>();
        instance = cfg;
        // 補上缺少的模組預設值
        for (Hud.Element e : Hud.ELEMENTS.values()) {
            cfg.modules.computeIfAbsent(e.id, k -> new ModCfg(e.defaultOn, e.defaultX, e.defaultY));
        }
        for (String id : new String[] {"sprint", "nightvision"}) {
            cfg.modules.computeIfAbsent(id, k -> new ModCfg(false, 0, 0));
        }
        cfg.modules.computeIfAbsent("zoom", k -> new ModCfg(true, 0, 0));
        cfg.modules.computeIfAbsent("uncap", k -> new ModCfg(true, 0, 0));
    }

    public static void save() {
        try {
            Files.createDirectories(FILE.getParent());
            try (Writer w = Files.newBufferedWriter(FILE)) {
                GSON.toJson(instance, w);
            }
        } catch (IOException e) {
            CubeNovaClient.LOGGER.warn("Could not save cubenova.json", e);
        }
    }

    public ModCfg of(String id) {
        return modules.computeIfAbsent(id, k -> new ModCfg(false, 0, 0));
    }

    public boolean on(String id) {
        ModCfg c = modules.get(id);
        return c != null && c.enabled;
    }
}
