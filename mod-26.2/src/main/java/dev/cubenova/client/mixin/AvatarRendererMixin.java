package dev.cubenova.client.mixin;

import dev.cubenova.client.CapeManager;
import net.minecraft.client.renderer.entity.player.AvatarRenderer;
import net.minecraft.client.renderer.entity.state.AvatarRenderState;
import net.minecraft.world.entity.Avatar;
import net.minecraft.world.entity.player.Player;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(AvatarRenderer.class)
public abstract class AvatarRendererMixin {
    /** Make sure the cape is drawn when we have a custom one, even if the account has no official cape. */
    @Inject(method = "extractRenderState", at = @At("TAIL"))
    private void cubenova$showCape(Avatar avatar, AvatarRenderState state, float partialTick, CallbackInfo ci) {
        try {
            if (avatar instanceof Player player && CapeManager.hasCape(player.getUUID())) state.showCape = true;
        } catch (Throwable ignored) {
        }
    }
}
