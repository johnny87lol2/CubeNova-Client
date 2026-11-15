package dev.cubenova.client.mixin;

import dev.cubenova.client.Freelook;
import net.minecraft.client.MouseHandler;
import net.minecraft.client.player.LocalPlayer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Redirect;

@Mixin(MouseHandler.class)
public abstract class MouseHandlerMixin {
    /** While freelook is active, mouse movement rotates the camera instead of the player. */
    @Redirect(method = "turnPlayer(D)V",
            at = @At(value = "INVOKE", target = "Lnet/minecraft/client/player/LocalPlayer;turn(DD)V"))
    private void cubenova$freelookTurn(LocalPlayer player, double yRot, double xRot) {
        if (Freelook.active) Freelook.turn(yRot, xRot);
        else player.turn(yRot, xRot);
    }
}
