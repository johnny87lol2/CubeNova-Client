package dev.cubenova.client.mixin;

import dev.cubenova.client.CapeManager;
import net.minecraft.client.player.AbstractClientPlayer;
import net.minecraft.resources.ResourceLocation;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** 1.20.1 has no PlayerSkin record: capes and elytra come from separate getters. */
@Mixin(AbstractClientPlayer.class)
public abstract class AbstractClientPlayerMixin {
    @Inject(method = "getCloakTextureLocation", at = @At("RETURN"), cancellable = true)
    private void cubenova$cape(CallbackInfoReturnable<ResourceLocation> cir) {
        try {
            ResourceLocation r = CapeManager.cape(((AbstractClientPlayer) (Object) this).getUUID(), cir.getReturnValue());
            if (r != cir.getReturnValue()) cir.setReturnValue(r);
        } catch (Throwable ignored) {
        }
    }

    @Inject(method = "getElytraTextureLocation", at = @At("RETURN"), cancellable = true)
    private void cubenova$elytra(CallbackInfoReturnable<ResourceLocation> cir) {
        try {
            ResourceLocation r = CapeManager.elytra(((AbstractClientPlayer) (Object) this).getUUID(), cir.getReturnValue());
            if (r != cir.getReturnValue()) cir.setReturnValue(r);
        } catch (Throwable ignored) {
        }
    }

    @Inject(method = "isCapeLoaded", at = @At("RETURN"), cancellable = true)
    private void cubenova$capeLoaded(CallbackInfoReturnable<Boolean> cir) {
        try {
            if (!cir.getReturnValueZ() && CapeManager.hasCape(((AbstractClientPlayer) (Object) this).getUUID())) cir.setReturnValue(true);
        } catch (Throwable ignored) {
        }
    }

    @Inject(method = "isElytraLoaded", at = @At("RETURN"), cancellable = true)
    private void cubenova$elytraLoaded(CallbackInfoReturnable<Boolean> cir) {
        try {
            if (!cir.getReturnValueZ() && CapeManager.hasElytra(((AbstractClientPlayer) (Object) this).getUUID())) cir.setReturnValue(true);
        } catch (Throwable ignored) {
        }
    }
}
