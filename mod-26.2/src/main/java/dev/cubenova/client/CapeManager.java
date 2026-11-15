package dev.cubenova.client;

import com.mojang.blaze3d.platform.NativeImage;
import net.fabricmc.loader.api.FabricLoader;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.core.ClientAsset;
import net.minecraft.resources.Identifier;
import net.minecraft.world.entity.player.PlayerSkin;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;

/** Loads the custom cape from config/cubenova_cape.png (copied there by the launcher). Only the local player gets it. */
public final class CapeManager {
    private CapeManager() {}

    private record CapeAsset(Identifier id) implements ClientAsset.Texture {
        @Override
        public Identifier texturePath() {
            return id;
        }
    }

    private static final Path FILE = FabricLoader.getInstance().getConfigDir().resolve("cubenova_cape.png");

    private static CapeAsset asset;
    private static long stamp = Long.MIN_VALUE;   // lastModified (or -1 when the file is missing) of what is loaded
    private static long nextCheck = 0;
    private static int counter = 0;
    private static PlayerSkin lastBase, lastResult;
    private static CapeAsset lastAsset;

    private static boolean isLocal(UUID uuid, Minecraft mc) {
        return mc.player != null && uuid != null && uuid.equals(mc.player.getUUID()) && HudConfig.get().on("capes");
    }

    /** True if this player is us and a custom cape is loaded. */
    public static boolean hasCape(UUID uuid) {
        Minecraft mc = Minecraft.getInstance();
        if (!isLocal(uuid, mc)) return false;
        reload(mc);
        return asset != null;
    }

    /** Returns the skin with the custom cape applied if this is the local player and a cape file exists. */
    public static PlayerSkin apply(UUID uuid, PlayerSkin base) {
        Minecraft mc = Minecraft.getInstance();
        if (base == null || !isLocal(uuid, mc)) return base;
        reload(mc);
        CapeAsset cape = asset;
        if (cape == null) return base;
        if (lastBase == base && lastAsset == cape) return lastResult;
        PlayerSkin r = new PlayerSkin(base.body(), cape, base.elytra(), base.model(), base.secure());
        lastBase = base;
        lastAsset = cape;
        lastResult = r;
        return r;
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
            CapeAsset old = asset;
            asset = null;
            if (old != null) mc.getTextureManager().release(old.id());
            if (m < 0) return;
            try (var in = Files.newInputStream(FILE)) {
                NativeImage img = NativeImage.read(in);
                Identifier id = Identifier.fromNamespaceAndPath("cubenova_client", "cape/local" + (counter++));
                mc.getTextureManager().register(id, new DynamicTexture(id::toString, img));
                asset = new CapeAsset(id);
            }
        } catch (Throwable t) {
            CubeNovaClient.LOGGER.warn("Could not load cubenova_cape.png", t);
        }
    }
}
