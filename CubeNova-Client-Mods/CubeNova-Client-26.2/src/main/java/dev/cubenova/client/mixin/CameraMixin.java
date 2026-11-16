package dev.cubenova.client.mixin;

import dev.cubenova.client.Freelook;
import net.minecraft.client.Camera;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Redirect;

@Mixin(Camera.class)
public abstract class CameraMixin {
    @Shadow
    protected abstract void setRotation(float yaw, float pitch);

    // alignWithEntity: the first setRotation call is for riding a minecart, the second for everything else.
    // The later calls flip the camera around for third-person front view and are left alone.
    @Redirect(method = "alignWithEntity",
            at = @At(value = "INVOKE", target = "Lnet/minecraft/client/Camera;setRotation(FF)V", ordinal = 0))
    private void cubenova$freelookMinecart(Camera camera, float yaw, float pitch) {
        cubenova$set(camera, yaw, pitch);
    }

    @Redirect(method = "alignWithEntity",
            at = @At(value = "INVOKE", target = "Lnet/minecraft/client/Camera;setRotation(FF)V", ordinal = 1))
    private void cubenova$freelookNormal(Camera camera, float yaw, float pitch) {
        cubenova$set(camera, yaw, pitch);
    }

    private void cubenova$set(Camera camera, float yaw, float pitch) {
        if (Freelook.active) ((CameraMixin) (Object) camera).setRotation(Freelook.yaw, Freelook.pitch);
        else ((CameraMixin) (Object) camera).setRotation(yaw, pitch);
    }
}
