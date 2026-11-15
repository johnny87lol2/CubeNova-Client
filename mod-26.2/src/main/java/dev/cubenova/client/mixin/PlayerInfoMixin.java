package dev.cubenova.client.mixin;

import com.mojang.authlib.GameProfile;
import dev.cubenova.client.CapeManager;
import net.minecraft.client.multiplayer.PlayerInfo;
import net.minecraft.world.entity.player.PlayerSkin;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(PlayerInfo.class)
public abstract class PlayerInfoMixin {
    @Shadow
    @Final
    private GameProfile profile;

    /** Swap in the local player's CubeNova custom cape (if there is one). */
    @Inject(method = "getSkin", at = @At("RETURN"), cancellable = true)
    private void cubenova$cape(CallbackInfoReturnable<PlayerSkin> cir) {
        try {
            PlayerSkin base = cir.getReturnValue();
            PlayerSkin skin = CapeManager.apply(profile.id(), base);
            if (skin != base) cir.setReturnValue(skin);
        } catch (Throwable ignored) {
        }
    }
}
