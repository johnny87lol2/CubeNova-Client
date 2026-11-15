package dev.cubenova.client.mixin;

import dev.cubenova.client.Freelook;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Entity.class)
public abstract class EntityMixin {
    /** While freelook is active, mouse movement rotates the camera instead of the player. */
    @Inject(method = "turn", at = @At("HEAD"), cancellable = true)
    private void cubenova$freelookTurn(double yRot, double xRot, CallbackInfo ci) {
        if (Freelook.active && (Object) this == Minecraft.getInstance().player) {
            Freelook.turn(yRot, xRot);
            ci.cancel();
        }
    }
}
