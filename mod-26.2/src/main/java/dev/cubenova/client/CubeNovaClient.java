package dev.cubenova.client;

import com.mojang.blaze3d.platform.InputConstants;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientLifecycleEvents;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.keymapping.v1.KeyMappingHelper;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayConnectionEvents;
import net.fabricmc.fabric.api.client.rendering.v1.hud.HudElementRegistry;
import net.minecraft.resources.Identifier;
import net.minecraft.client.KeyMapping;
import net.minecraft.client.Minecraft;
import org.lwjgl.glfw.GLFW;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class CubeNovaClient implements ClientModInitializer {
    public static final Logger LOGGER = LoggerFactory.getLogger("CubeNova");

    public static KeyMapping menuKey;
    public static KeyMapping zoomKey;
    public static KeyMapping freelookKey;

    /** 關閉 VSync 並把 FPS 上限設為無限 (260 = 無限) */
    public static void applyPerformance(Minecraft mc) {
        if (mc == null || !HudConfig.get().on("uncap")) return;
        try {
            mc.options.enableVsync().set(false);
            mc.options.framerateLimit().set(260);
        } catch (Exception e) {
            LOGGER.warn("Could not apply performance settings", e);
        }
    }

    /** 開啟 Zoom 時，把設定裡其他佔用同一個按鍵(預設是 C 的「儲存快捷列」)改成未綁定，讓這個鍵只用來縮放 */
    public static void freeZoomKey(Minecraft mc) {
        if (mc == null || zoomKey == null || zoomKey.isUnbound() || !HudConfig.get().on("zoom")) return;
        String zk = zoomKey.saveString();
        boolean changed = false;
        for (KeyMapping km : mc.options.keyMappings) {
            if (km == zoomKey || km == menuKey) continue;
            if (!km.isUnbound() && km.saveString().equals(zk)) {
                km.setKey(InputConstants.UNKNOWN);
                changed = true;
                LOGGER.info("Unbound '{}' because the zoom key is {}", km.getName(), zk);
            }
        }
        if (changed) {
            KeyMapping.resetMapping();
            mc.options.save();
        }
    }

    @Override
    public void onInitializeClient() {
        KeyMapping.Category category = KeyMapping.Category.register(Identifier.fromNamespaceAndPath("cubenova_client", "main"));
        menuKey = KeyMappingHelper.registerKeyMapping(
                new KeyMapping("key.cubenova.menu", InputConstants.Type.KEYSYM, GLFW.GLFW_KEY_RIGHT_SHIFT, category));
        zoomKey = KeyMappingHelper.registerKeyMapping(
                new KeyMapping("key.cubenova.zoom", InputConstants.Type.KEYSYM, GLFW.GLFW_KEY_C, category));

        freelookKey = KeyMappingHelper.registerKeyMapping(
                new KeyMapping("key.cubenova.freelook", InputConstants.Type.KEYSYM, GLFW.GLFW_KEY_LEFT_ALT, category));

        HudConfig.load();

        ClientLifecycleEvents.CLIENT_STARTED.register(client -> freeZoomKey(client));

        // 進入世界/伺服器時：關閉垂直同步、FPS 上限設為無限
        ClientPlayConnectionEvents.JOIN.register((handler, sender, client) -> client.execute(() -> {
            applyPerformance(client);
            freeZoomKey(client);
        }));

        HudElementRegistry.addLast(Identifier.fromNamespaceAndPath("cubenova_client", "hud"),
                (graphics, deltaTracker) -> Hud.render(graphics));

        ClientTickEvents.END_CLIENT_TICK.register(client -> {
            Hud.tick(client);
            Freelook.tick(client);
            while (menuKey.consumeClick()) {
                if (client.gui.screen() == null) client.gui.setScreen(new HudScreen());
            }
        });

        LOGGER.info("CubeNova Client loaded");
    }
}
