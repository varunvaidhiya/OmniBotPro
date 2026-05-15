#!/usr/bin/env python3
"""
Train OmniBot arm manipulation RL policy using Isaac Lab + RSL-RL (PPO).

Prerequisites
─────────────
Same as train_nav.py — requires Isaac Lab + Isaac Sim installation.

Usage
─────
  # Train with default config (256 envs, 3000 iterations)
  python rl_engine/scripts/train_arm.py --num_envs 256

  # Resume from checkpoint
  python rl_engine/scripts/train_arm.py --resume ~/logs/omnibot_arm

  # After training, export policy:
  python rl_engine/export/export_policy.py \\
    --checkpoint ~/logs/omnibot_arm/checkpoints/model_3000.pt \\
    --output ~/models/omnibot_arm_policy.onnx --type arm
"""

import argparse
import os
import sys

try:
    from isaaclab.app import AppLauncher

    parser = argparse.ArgumentParser(description='Train OmniBot arm policy with Isaac Lab')
    AppLauncher.add_app_launcher_args(parser)
    parser.add_argument('--num_envs',       type=int,  default=256)
    parser.add_argument('--max_iterations', type=int,  default=3000)
    parser.add_argument('--log_dir',        type=str,  default='~/logs/omnibot_arm')
    parser.add_argument('--resume',         type=str,  default=None)
    parser.add_argument('--seed',           type=int,  default=42)
    args = parser.parse_args()

    app_launcher = AppLauncher(args)
    simulation_app = app_launcher.app

except ImportError:
    print('ERROR: Isaac Lab not found. Install from https://isaac-sim.github.io/IsaacLab/')
    sys.exit(1)


def main():
    import yaml
    import torch
    from isaaclab_tasks.utils.wrappers.rsl_rl import (
        RslRlOnPolicyRunnerCfg,
        RslRlVecEnvWrapper,
    )
    from rsl_rl.runners import OnPolicyRunner

    from rl_engine.envs.omnibot_arm_env import OmnibotArmEnvCfg

    config_path = os.path.join(
        os.path.dirname(__file__), '..', 'config', 'arm_train.yaml')
    with open(config_path) as f:
        train_cfg = yaml.safe_load(f)

    ppo_cfg = train_cfg['ppo']
    net_cfg = train_cfg['network']

    # ── Environment ────────────────────────────────────────────────────────
    env_cfg = OmnibotArmEnvCfg()
    env_cfg.scene.num_envs = args.num_envs
    env_cfg.seed = args.seed

    from isaaclab.envs import ManagerBasedRLEnv
    env = ManagerBasedRLEnv(cfg=env_cfg)
    env = RslRlVecEnvWrapper(env)

    # ── Runner ─────────────────────────────────────────────────────────────
    runner_cfg = RslRlOnPolicyRunnerCfg(
        num_steps_per_env=24,
        max_iterations=args.max_iterations,
        save_interval=train_cfg['training']['save_interval'],
        experiment_name='omnibot_arm',
        log_dir=os.path.expanduser(args.log_dir),
        resume=args.resume is not None,
        load_run=args.resume or '',
    )

    actor_critic_cfg = {
        'class_name': 'ActorCritic',
        'actor_hidden_dims':  net_cfg['actor_hidden_dims'],
        'critic_hidden_dims': net_cfg['critic_hidden_dims'],
        'activation':         net_cfg['activation'],
        'init_noise_std':     net_cfg['init_noise_std'],
    }
    alg_cfg = {
        'class_name': 'PPO',
        **{k: ppo_cfg[k] for k in (
            'value_loss_coef', 'use_clipped_value_loss', 'clip_param',
            'entropy_coef', 'num_learning_epochs', 'num_mini_batches',
            'learning_rate', 'schedule', 'gamma', 'lam',
            'desired_kl', 'max_grad_norm')},
    }

    runner = OnPolicyRunner(
        env,
        {'runner': runner_cfg.__dict__, 'policy': actor_critic_cfg, 'algorithm': alg_cfg},
        log_dir=os.path.expanduser(args.log_dir),
        device='cuda' if torch.cuda.is_available() else 'cpu',
    )

    if args.resume:
        runner.load(args.resume)

    print(f'Starting arm policy training: {args.max_iterations} iterations, '
          f'{args.num_envs} environments.')
    runner.learn(num_learning_iterations=args.max_iterations, init_at_random_ep_len=True)

    env.close()
    simulation_app.close()


if __name__ == '__main__':
    main()
