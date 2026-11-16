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

/**
 * Custom cape and elytra for the local player. The launcher copies config/cubenova_cape.png and
 * config/cubenova_elytra.png; with only a cape the elytra falls back to the normal vanilla elytra
 * (vanilla would otherwise draw the cape texture on the elytra, which makes it invisible).
 */
public final class CapeManager {
    private CapeManager() {}

    private record Asset(Identifier id) implements ClientAsset.Texture {
        @Override
        public Identifier texturePath() {
            return id;
        }
    }

    /** One texture file that is watched and (re)loaded when it changes. */
    private static final class Slot {
        final Path file;
        final String prefix;
        Asset asset;
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
                Asset old = asset;
                asset = null;
                if (old != null) mc.getTextureManager().release(old.id());
                if (m < 0) return;
                try (var in = Files.newInputStream(file)) {
                    NativeImage img = NativeImage.read(in);
                    Identifier id = Identifier.fromNamespaceAndPath("cubenova_client", prefix + "/local" + (counter++));
                    mc.getTextureManager().register(id, new DynamicTexture(id::toString, img));
                    asset = new Asset(id);
                }
            } catch (Throwable t) {
                CubeNovaClient.LOGGER.warn("Could not load " + file.getFileName(), t);
            }
        }
    }

    private static final Slot CAPE = new Slot("cubenova_cape.png", "cape");
    private static final Slot ELYTRA = new Slot("cubenova_elytra.png", "elytra");

    private static Asset vanillaElytra;
    private static boolean vanillaElytraChecked;

    private static PlayerSkin lastBase, lastResult;
    private static Asset lastCape, lastElytra;

    private static boolean isLocal(UUID uuid, Minecraft mc) {
        return mc.player != null && uuid != null && uuid.equals(mc.player.getUUID()) && HudConfig.get().on("capes");
    }

    /** The normal elytra texture (its path depends on the game version, so check which one exists). */
    private static Asset vanillaElytra(Minecraft mc) {
        if (!vanillaElytraChecked) {
            vanillaElytraChecked = true;
            for (String p : new String[] {"textures/entity/equipment/wings/elytra.png", "textures/entity/elytra.png"}) {
                Identifier id = Identifier.withDefaultNamespace(p);
                if (mc.getResourceManager().getResource(id).isPresent()) {
                    vanillaElytra = new Asset(id);
                    break;
                }
            }
        }
        return vanillaElytra;
    }

    /** True if this player is us and a custom cape is loaded (used to force the cape to be drawn). */
    public static boolean hasCape(UUID uuid) {
        Minecraft mc = Minecraft.getInstance();
        if (!isLocal(uuid, mc)) return false;
        CAPE.reload(mc);
        return CAPE.asset != null;
    }

    /** Returns the skin with the custom cape / elytra applied if this is the local player and files exist. */
    public static PlayerSkin apply(UUID uuid, PlayerSkin base) {
        Minecraft mc = Minecraft.getInstance();
        if (base == null || !isLocal(uuid, mc)) return base;
        CAPE.reload(mc);
        ELYTRA.reload(mc);
        Asset cape = CAPE.asset, elytra = ELYTRA.asset;
        if (cape == null && elytra == null) return base;
        if (lastBase == base && lastCape == cape && lastElytra == elytra) return lastResult;
        ClientAsset.Texture useCape = cape != null ? cape : base.cape();
        ClientAsset.Texture useElytra = base.elytra();
        if (elytra != null) useElytra = elytra;
        else if (cape != null && useElytra == null) useElytra = vanillaElytra(mc);   // plain elytra instead of the cape on the wings
        PlayerSkin r = new PlayerSkin(base.body(), useCape, useElytra, base.model(), base.secure());
        lastBase = base;
        lastCape = cape;
        lastElytra = elytra;
        lastResult = r;
        return r;
    }
}
