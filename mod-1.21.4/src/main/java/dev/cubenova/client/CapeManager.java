package dev.cubenova.client;

import com.mojang.blaze3d.platform.NativeImage;
import net.fabricmc.loader.api.FabricLoader;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.client.resources.PlayerSkin;
import net.minecraft.resources.ResourceLocation;

import java.lang.reflect.RecordComponent;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;

/** Loads the custom cape from config/cubenova_cape.png (copied there by the launcher). Only the local player gets it. */
public final class CapeManager {
    private CapeManager() {}

    private static final Path FILE = FabricLoader.getInstance().getConfigDir().resolve("cubenova_cape.png");

    private static ResourceLocation tex;
    private static long stamp = Long.MIN_VALUE;   // lastModified (or -1 when the file is missing) of what is loaded
    private static long nextCheck = 0;
    private static int counter = 0;
    private static PlayerSkin lastBase, lastResult;
    private static ResourceLocation lastCape;

    /** Returns the skin with the custom cape applied if this is the local player and a cape file exists. */
    public static PlayerSkin apply(UUID uuid, PlayerSkin base) {
        Minecraft mc = Minecraft.getInstance();
        if (base == null || mc.player == null || !uuid.equals(mc.player.getUUID()) || !HudConfig.get().on("capes")) return base;
        reload(mc);
        ResourceLocation cape = tex;
        if (cape == null) return base;
        if (lastBase == base && lastCape == cape) return lastResult;
        try {
            PlayerSkin r = withCape(base, cape);
            lastBase = base;
            lastCape = cape;
            lastResult = r;
            return r;
        } catch (Throwable t) {
            CubeNovaClient.LOGGER.warn("Could not apply cape", t);
            tex = null;
            stamp = Long.MAX_VALUE;
            return base;
        }
    }

    /** Checks the file every 3 seconds and (re)loads the texture when it changed. */
    private static void reload(Minecraft mc) {
        long now = System.currentTimeMillis();
        if (now < nextCheck) return;
        nextCheck = now + 3000;
        try {
            long m = Files.exists(FILE) ? Files.getLastModifiedTime(FILE).toMillis() : -1;
            if (m == stamp) return;
            stamp = m;
            ResourceLocation old = tex;
            tex = null;
            if (old != null) mc.getTextureManager().release(old);
            if (m < 0) return;
            try (var in = Files.newInputStream(FILE)) {
                NativeImage img = NativeImage.read(in);
                ResourceLocation id = ResourceLocation.fromNamespaceAndPath("cubenova_client", "cape/local" + (counter++));
                mc.getTextureManager().register(id, new DynamicTexture(img));
                tex = id;
            }
        } catch (Throwable t) {
            CubeNovaClient.LOGGER.warn("Could not load cubenova_cape.png", t);
        }
    }

    /** Rebuilds the skin record with a different cape, finding the cape by type order (texture, cape, elytra). */
    private static PlayerSkin withCape(PlayerSkin skin, ResourceLocation cape) throws Exception {
        RecordComponent[] rc = PlayerSkin.class.getRecordComponents();
        Class<?>[] types = new Class<?>[rc.length];
        Object[] args = new Object[rc.length];
        int rl = 0;
        for (int i = 0; i < rc.length; i++) {
            types[i] = rc[i].getType();
            args[i] = rc[i].getAccessor().invoke(skin);
            if (types[i] == ResourceLocation.class && rl++ == 1) args[i] = cape;
        }
        return (PlayerSkin) PlayerSkin.class.getDeclaredConstructor(types).newInstance(args);
    }
}
