/*
 * Shared vertical-offset state for the 3-D robot layer.
 *
 * The robot background is now fixed and does not move on scroll, so this stays
 * 0. OmniBotModel still reads it when mapping the cursor → floor projection;
 * keeping the hook here means re-introducing a layer shift later only requires
 * writing to offsetPx, with the cursor math already accounting for it.
 */
export const robotParallax = { offsetPx: 0 };
