"""
Custom Isaac Lab action terms for OmniBot.

MecanumWheelActionTerm
  Converts body-frame velocity commands (vx, vy, omega) to individual wheel
  angular velocity targets using the exact OmniBot inverse kinematics.
  Constants from CLAUDE.md and confirmed_protocol.py.

ArmJointDeltaActionTerm
  Converts joint position deltas to absolute position targets, accumulating
  from the current joint state. Clips to URDF joint limits.
"""

import math

import torch
from isaaclab.envs import ManagerBasedRLEnv
from isaaclab.managers import ActionTerm, ActionTermCfg
from dataclasses import dataclass, field
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    pass


# ── OmniBot mechanical constants (from CLAUDE.md) ────────────────────────────
WHEEL_RADIUS        = 0.040   # m — all 4 wheels
WHEEL_SEP_LENGTH    = 0.165   # m — front to rear (lx = 0.0825)
WHEEL_SEP_WIDTH     = 0.215   # m — left to right (ly = 0.1075)
LX = WHEEL_SEP_LENGTH / 2.0  # 0.0825 m
LY = WHEEL_SEP_WIDTH  / 2.0  # 0.1075 m
L  = LX + LY                  # 0.19 m (used in inverse kinematics)
R  = WHEEL_RADIUS

MAX_LIN_VEL = 0.20   # m/s   — hardware limit
MAX_ANG_VEL = 1.00   # rad/s — hardware limit
MAX_DELTA   = 0.05   # m/s per step — mirrors Yahboom ramp limiter at 20 Hz

# SO-101 joint limits (rad) — from arm_params.yaml / URDF
ARM_JOINT_MIN = torch.tensor([-3.14, -1.57, -1.69, -1.66, -2.74, -0.17])
ARM_JOINT_MAX = torch.tensor([ 3.14,  1.57,  1.69,  1.66,  2.84,  1.75])
ARM_MAX_DELTA = 0.05  # rad/step


class MecanumWheelActionTerm(ActionTerm):
    """
    Converts 3D body-frame velocity deltas (Δvx, Δvy, Δω) to 4 wheel
    angular velocity targets using OmniBot's exact inverse kinematics.

    The policy outputs deltas to mirror the Yahboom board's 0.05 m/s ramp
    limiter. This ensures the sim action space matches real hardware behaviour.

    Inverse kinematics (from confirmed_protocol.py):
      ω_FL = (vx - vy - L*ω) / R
      ω_FR = (vx + vy + L*ω) / R
      ω_RL = (vx + vy - L*ω) / R
      ω_RR = (vx - vy + L*ω) / R
    """

    cfg: 'MecanumWheelActionTermCfg'

    def __init__(self, cfg: 'MecanumWheelActionTermCfg', env: ManagerBasedRLEnv):
        super().__init__(cfg, env)
        self._num_envs = env.num_envs
        self._device   = env.device
        # Accumulated body velocity per environment
        self._vel_accum = torch.zeros(self._num_envs, 3, device=self._device)

    @property
    def action_dim(self) -> int:
        return 3   # [Δvx, Δvy, Δω]

    def process_actions(self, actions: torch.Tensor) -> None:
        """Accumulate velocity deltas, clip to hardware limits."""
        delta = actions.clamp(-MAX_DELTA, MAX_DELTA)
        self._vel_accum += delta
        self._vel_accum[:, :2].clamp_(-MAX_LIN_VEL, MAX_LIN_VEL)
        self._vel_accum[:, 2].clamp_(-MAX_ANG_VEL, MAX_ANG_VEL)

    def apply_actions(self) -> None:
        """Convert accumulated body velocity to wheel velocities and write to articulation."""
        vx    = self._vel_accum[:, 0]
        vy    = self._vel_accum[:, 1]
        omega = self._vel_accum[:, 2]

        # OmniBot inverse kinematics (exact constants)
        w_fl = (vx - vy - L * omega) / R
        w_fr = (vx + vy + L * omega) / R
        w_rl = (vx + vy - L * omega) / R
        w_rr = (vx - vy + L * omega) / R

        # Stack: [FL, FR, RL, RR] — must match wheel joint order in USD
        wheel_vels = torch.stack([w_fl, w_fr, w_rl, w_rr], dim=1)

        # Write velocity targets to articulation (joint indices set by env)
        self._asset.set_joint_velocity_target(
            wheel_vels, joint_ids=self.cfg.wheel_joint_ids)

    def reset(self, env_ids: torch.Tensor | None = None) -> None:
        if env_ids is None:
            self._vel_accum.zero_()
        else:
            self._vel_accum[env_ids] = 0.0


@dataclass
class MecanumWheelActionTermCfg(ActionTermCfg):
    """Configuration for MecanumWheelActionTerm."""
    class_type: type = MecanumWheelActionTerm
    # Joint IDs in the articulation corresponding to [FL, FR, RL, RR] wheels
    # Set these to match your USD articulation joint ordering.
    wheel_joint_ids: list[int] = field(default_factory=lambda: [0, 1, 2, 3])


# ── Arm Joint Delta Action Term ───────────────────────────────────────────────

class ArmJointDeltaActionTerm(ActionTerm):
    """
    Converts 6D joint position deltas to absolute position targets.

    Accumulates from the current joint state at episode start, enforcing
    URDF joint limits per step. Maps directly to STS3215 Goal_Position commands.
    """

    cfg: 'ArmJointDeltaActionTermCfg'

    def __init__(self, cfg: 'ArmJointDeltaActionTermCfg', env: ManagerBasedRLEnv):
        super().__init__(cfg, env)
        self._num_envs  = env.num_envs
        self._device    = env.device
        self._joint_min = ARM_JOINT_MIN.to(self._device)
        self._joint_max = ARM_JOINT_MAX.to(self._device)
        # Accumulated joint targets (initialised from current state on reset)
        self._joint_targets = torch.zeros(self._num_envs, 6, device=self._device)

    @property
    def action_dim(self) -> int:
        return 6   # [Δq0 … Δq5]

    def process_actions(self, actions: torch.Tensor) -> None:
        """Clip delta and accumulate joint targets, enforcing limits."""
        delta = actions.clamp(-ARM_MAX_DELTA, ARM_MAX_DELTA)
        self._joint_targets += delta
        self._joint_targets.clamp_(
            self._joint_min.unsqueeze(0),
            self._joint_max.unsqueeze(0))

    def apply_actions(self) -> None:
        """Write position targets to arm articulation."""
        self._asset.set_joint_position_target(
            self._joint_targets, joint_ids=self.cfg.arm_joint_ids)

    def reset(self, env_ids: torch.Tensor | None = None) -> None:
        """Initialise targets from current joint state on episode reset."""
        if env_ids is None:
            env_ids = torch.arange(self._num_envs, device=self._device)
        current_pos = self._asset.data.joint_pos[env_ids]
        if current_pos.shape[-1] >= 6:
            self._joint_targets[env_ids] = current_pos[
                :, self.cfg.arm_joint_ids].clamp(
                self._joint_min, self._joint_max)


@dataclass
class ArmJointDeltaActionTermCfg(ActionTermCfg):
    """Configuration for ArmJointDeltaActionTerm."""
    class_type: type = ArmJointDeltaActionTerm
    # Joint IDs in the articulation for the 6 arm joints
    arm_joint_ids: list[int] = field(default_factory=lambda: [4, 5, 6, 7, 8, 9])
