package net.cubenova.client;

import net.cubenova.client.feature.BlockOutline;
import net.cubenova.client.feature.FreeLook;
import net.cubenova.client.feature.Stats;
import net.cubenova.client.feature.Zoom;
import net.cubenova.client.hud.Hud;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.keybinding.v1.KeyBindingHelper;
import net.fabricmc.fabric.api.client.rendering.v1.HudRenderCallback;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderEvents;
import net.fabricmc.fabric.api.event.player.AttackEntityCallback;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.option.KeyBinding;
import net.minecraft.client.util.InputUtil;
import net.minecraft.util.ActionResult;
import org.lwjgl.glfw.GLFW;

public class CubeNovaClient implements ClientModInitializer {
    public static final String CATEGORY = "key.categories.cubenova";
    public static KeyBinding menuKey, zoomKey, freelookKey;

    @Override
    public void onInitializeClient() {
        Config.load();
        Hud.init();

        // Defaults for non-HUD modules (so they show up in the menu and config file)
        Config.module("zoom", true, 0, 0);
        Config.module("freelook", true, 0, 0);
        Config.module("crosshair", false, 0, 0);
        Config.module("blockoutline", true, 0, 0);
        Config.save();

        menuKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
            "key.cubenova.menu", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_RIGHT_SHIFT, CATEGORY));
        zoomKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
            "key.cubenova.zoom", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_C, CATEGORY));
        freelookKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
            "key.cubenova.freelook", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_LEFT_ALT, CATEGORY));

        ClientTickEvents.END_CLIENT_TICK.register(CubeNovaClient::tick);
        HudRenderCallback.EVENT.register(Hud::render);
        WorldRenderEvents.BLOCK_OUTLINE.register(BlockOutline::render);

        AttackEntityCallback.EVENT.register((player, world, hand, entity, hit) -> {
            if (world.isClient) Stats.onAttack(player, entity);
            return ActionResult.PASS;
        });
    }

    private static void tick(MinecraftClient mc) {
        while (menuKey.wasPressed()) {
            if (mc.currentScreen == null) mc.setScreen(new ModMenuScreen());
        }
        Zoom.setActive(zoomKey.isPressed() && mc.currentScreen == null);
        Zoom.tick();
        FreeLook.update(mc, freelookKey.isPressed());
        Stats.tick(mc.player);
    }
}
