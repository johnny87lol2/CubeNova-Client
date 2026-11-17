package dev.cubenova.client.mixin;

import dev.cubenova.client.CapeManager;
import net.minecraft.client.player.AbstractClientPlayer;
import net.minecraft.client.resources.PlayerSkin;
import net.minecraft.world.entity.Entity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(AbstractClientPlayer.class)
public abstract class AbstractClientPlayerMixin {
    /** Swap in the player's CubeNova custom cape (if they have one). */
    @Inject(method = "getSkin", at = @At("RETURN"), cancellable = true)
    private void cubenova$cape(CallbackInfoReturnable<PlayerSkin> cir) {
        try {
            PlayerSkin base = cir.getReturnValue();
            PlayerSkin skin = CapeManager.apply(((Entity) (Object) this).getUUID(), base);
            if (skin != base) cir.setReturnValue(skin);
        } catch (Throwable ignored) {
        }
    }
}
