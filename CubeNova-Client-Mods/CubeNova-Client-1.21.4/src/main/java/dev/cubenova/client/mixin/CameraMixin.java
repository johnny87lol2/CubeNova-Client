package dev.cubenova.client.mixin;

import dev.cubenova.client.Freelook;
import net.minecraft.client.Camera;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.ModifyArgs;
import org.spongepowered.asm.mixin.injection.invoke.arg.Args;

@Mixin(Camera.class)
public abstract class CameraMixin {
    /** The first setRotation call in setup() uses the entity's rotation; replace it with the freelook rotation. */
    @ModifyArgs(method = "setup",
            at = @At(value = "INVOKE", target = "Lnet/minecraft/client/Camera;setRotation(FF)V", ordinal = 0))
    private void cubenova$freelookRotation(Args args) {
        if (Freelook.active) {
            args.set(0, Freelook.yaw);
            args.set(1, Freelook.pitch);
        }
    }
}
