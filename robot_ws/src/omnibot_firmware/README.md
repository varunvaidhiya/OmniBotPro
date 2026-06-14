# OmniBot STM32 Firmware (LEGACY — not active)

Firmware for an STM32F4 board, retained from the original STM32-based drive
chain. **The active robot uses the Yahboom expansion board** driven by
`omnibot_driver` (`yahboom_controller_node`) over USB serial; this firmware is
not part of the active build. See the repo-root `MIGRATION_GUIDE.md` for the
STM32 → Yahboom migration.

This is **not** a ROS 2 package — there is no `package.xml` and it is not built
by `colcon`.

## What the current firmware does

`Core/Src/main.c` initializes peripherals and runs a simple loop that:

1. Starts PWM on TIM2–TIM5 (two channels each — one per motor direction pin).
2. Starts the four encoder interfaces on TIM1, TIM8, TIM9, TIM10.
3. Every 10 ms reads the four encoder counters (`TIMx->CNT`) and transmits
   them to the host over UART2 (115200 baud, 8N1):

   ```
   <ENCODERS,fl,fr,rl,rr>\n     # raw 32-bit timer counts (ticks)
   ```

> The encoder values are raw timer counts, not rad/s. There is **no UART
> receive / command-parsing code** in the current sources — the motor command
> and heartbeat protocol described in earlier revisions is not implemented, and
> `wheel_velocities[]` is declared but never applied to the PWM outputs.

## Build files

Only a partial CubeMX-style tree is checked in (`Core/Inc/main.h`,
`Core/Src/{main,gpio,tim,usart}.c`). There is no project/IDE file or build
script in the repo; it must be reconstructed in STM32CubeIDE / CubeMX before it
can be built or flashed.

## Pin / peripheral map (from `Core/Inc/main.h` + `Core/Src/tim.c`)

### PWM (motor speed) — TIM2–TIM5, period 8399

| Motor | PWM pins |
|---|---|
| M1 | `M1_PWM1` (PIN_0), `M1_PWM2` (PIN_1) |
| M2 | `M2_PWM1` (PIN_2), `M2_PWM2` (PIN_3) |
| M3 | `M3_PWM1` (PIN_6), `M3_PWM2` (PIN_7) |
| M4 | `M4_PWM1` (PIN_8), `M4_PWM2` (PIN_9) |

### Direction pins

`M1_DIR1/2` … `M4_DIR1/2` on PIN_0–PIN_7.

### Encoders — TIM1, TIM8, TIM9, TIM10 (period 65535)

`ENC1_A/B` … `ENC4_A/B` on PIN_0–PIN_7.

### Serial

UART2 — `USART_TX` (PIN_2), `USART_RX` (PIN_3), 115200 baud → host.

### Status LED

`LED` on PIN_12.

## Flashing

There is no provided build artifact. After reconstructing and building the
project in STM32CubeIDE, flash via ST-LINK, e.g.:

```bash
openocd -f interface/stlink.cfg -f target/stm32f4x.cfg \
  -c "program build/<firmware>.elf verify reset exit"
```
