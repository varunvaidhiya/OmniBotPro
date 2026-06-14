/*
 * Shared scroll-parallax state for the 3-D robot layer.
 *
 * RobotBackground writes the current vertical offset (in px) that the robot
 * layer is translated by as the page scrolls; OmniBotModel reads it so the
 * cursor → floor projection stays accurate even while the layer is shifted
 * up/down off the viewport.
 */
export const robotParallax = { offsetPx: 0 };
