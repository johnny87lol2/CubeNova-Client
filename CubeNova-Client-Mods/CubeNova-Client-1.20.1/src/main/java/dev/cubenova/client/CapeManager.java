package dev.cubenova.client;

import com.mojang.blaze3d.platform.NativeImage;
import net.fabricmc.loader.api.FabricLoader;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.resources.ResourceLocation;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;

/**
 * Custom cape and elytra for the local player (Minecraft 1.20.1). The launcher copies config/cubenova_cape.png
 * and config/cubenova_elytra.png. With only a cape the elytra stays the normal vanilla elytra
 * (vanilla would otherwise draw the cape texture on the wings, which makes them invisible).
 */
public final class CapeManager {
    private CapeManager() {}

    /** One texture file that is watched and (re)loaded when it changes. */
    private static final class Slot {
        final Path file;
        final String prefix;
        ResourceLocation tex;
        long stamp = Long.MIN_VALUE;   // lastModified (or -1 when missing) of what is loaded
        long next = 0;
        int counter = 0;

        Slot(String fileName, String prefix) {
            this.file = FabricLoader.getInstance().getConfigDir().resolve(fileName);
            this.prefix = prefix;
        }

        void reload(Minecraft mc) {
            long now = System.currentTimeMillis();
            if (now < next) return;
            next = now + 3000;
            try {
                long m = Files.exists(file) ? Files.getLastModifiedTime(file).toMillis() : -1;
                if (m == stamp) return;
                stamp = m;
                ResourceLocation old = tex;
                tex = null;
                if (old != null) mc.getTextureManager().release(old);
                if (m < 0) return;
                try (var in = Files.newInputStream(file)) {
                    NativeImage img = NativeImage.read(in);
                    ResourceLocation id = new ResourceLocation("cubenova_client", prefix + "/local" + (counter++));
                    mc.getTextureManager().register(id, new DynamicTexture(img));
                    tex = id;
                }
            } catch (Throwable t) {
                CubeNovaClient.LOGGER.warn("Could not load " + file.getFileName(), t);
            }
        }
    }

    private static final Slot CAPE = new Slot("cubenova_cape.png", "cape");
    private static final Slot ELYTRA = new Slot("cubenova_elytra.png", "elytra");
    private static final ResourceLocation VANILLA_ELYTRA = new ResourceLocation("textures/entity/elytra.png");

    private static boolean isLocal(UUID uuid, Minecraft mc) {
        return mc.player != null && uuid != null && uuid.equals(mc.player.getUUID()) && HudConfig.get().on("capes");
    }

    public static boolean hasCape(UUID uuid) {
        Minecraft mc = Minecraft.getInstance();
        if (!isLocal(uuid, mc)) return false;
        CAPE.reload(mc);
        return CAPE.tex != null;
    }

    public static boolean hasElytra(UUID uuid) {
        Minecraft mc = Minecraft.getInstance();
        if (!isLocal(uuid, mc)) return false;
        CAPE.reload(mc);
        ELYTRA.reload(mc);
        return ELYTRA.tex != null || CAPE.tex != null;
    }

    /** The cape texture to use for this player (the custom one for the local player, otherwise {@code original}). */
    public static ResourceLocation cape(UUID uuid, ResourceLocation original) {
        Minecraft mc = Minecraft.getInstance();
        if (!isLocal(uuid, mc)) return original;
        CAPE.reload(mc);
        return CAPE.tex != null ? CAPE.tex : original;
    }

    /** The elytra texture: custom elytra if set, else the plain vanilla elytra when a custom cape is used. */
    public static ResourceLocation elytra(UUID uuid, ResourceLocation original) {
        Minecraft mc = Minecraft.getInstance();
        if (!isLocal(uuid, mc)) return original;
        CAPE.reload(mc);
        ELYTRA.reload(mc);
        if (ELYTRA.tex != null) return ELYTRA.tex;
        if (CAPE.tex != null && original == null) return VANILLA_ELYTRA;
        return original;
    }
}
