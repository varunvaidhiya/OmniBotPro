#!/usr/bin/env python3
"""
Train OmniBot navigation RL policy using Isaac Lab + RSL-RL (PPO).

Prerequisites
─────────────
1. Isaac Lab installed and sourced:
     source IsaacLab/_isaac_sim/setup_conda_env.sh
     conda activate isaaclab

2. OmniBot USD available at the path in omnibot_nav_env.py:
     IsaacLab/scripts/tools/convert_urdf.py \\
       robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro \\
       robot_ws/src/omnibot_description/usd/omnibot.usd

Usage
─────
  # Train with default config (512 envs, 2000 iterations)
  python rl_engine/scripts/train_nav.py --num_envs 512

  # Resume from checkpoint
  python rl_engine/scripts/train_nav.py --resume ~/logs/omnibot_nav

  # Headless (no GUI, for remote server)
  python rl_engine/scripts/train_nav.py --headless --num_envs 1024

  # After training, export policy:
  python rl_engine/export/export_policy.py \\
    --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \\
    --output ~/models/omnibot_nav_policy.onnx --type nav
"""

import argparse
import os
import sys

# Isaac Lab app launcher must be initialized before any omni imports
try:
    from isaaclab.app import AppLauncher

    parser = argparse.ArgumentParser(description='Train OmniBot nav policy with Isaac Lab')
    AppLauncher.add_app_launcher_args(parser)
    parser.add_argument('--num_envs',       type=int,   default=512)
    parser.add_argument('--max_iterations', type=int,   default=2000)
    parser.add_argument('--log_dir',        type=str,   default='~/logs/omnibot_nav')
    parser.add_argument('--resume',         type=str,   default=None,
                        help='Path to run directory to resume from')
    parser.add_argument('--seed',           type=int,   default=42)
    args = parser.parse_args()

    app_launcher = AppLauncher(args)
    simulation_app = app_launcher.app

except ImportError:
    print('ERROR: Isaac Lab not found. Install from https://isaac-sim.github.io/IsaacLab/')
    print('       This script requires a full Isaac Sim + Isaac Lab installation.')
    sys.exit(1)


def main():
    import yaml
    import torch
    from isaaclab_tasks.utils.wrappers.rsl_rl import (
        RslRlOnPolicyRunnerCfg,
        RslRlVecEnvWrapper,
    )
    from rsl_rl.runners import OnPolicyRunner

    from rl_engine.envs.omnibot_nav_env import OmnibotNavEnvCfg

    # ── Load training config ───────────────────────────────────────────────
    config_path = os.path.join(
        os.path.dirname(__file__), '..', 'config', 'nav_train.yaml')
    with open(config_path) as f:
        train_cfg = yaml.safe_load(f)

    ppo_cfg = train_cfg['ppo']
    net_cfg = train_cfg['network']

    # ── Build environment ──────────────────────────────────────────────────
    env_cfg = OmnibotNavEnvCfg()
    env_cfg.scene.num_envs = args.num_envs
    env_cfg.seed = args.seed

    from isaaclab.envs import ManagerBasedRLEnv
    env = ManagerBasedRLEnv(cfg=env_cfg)
    env = RslRlVecEnvWrapper(env)

    # ── RSL-RL runner config ───────────────────────────────────────────────
    runner_cfg = RslRlOnPolicyRunnerCfg(
        num_steps_per_env=24,
        max_iterations=args.max_iterations,
        save_interval=train_cfg['training']['save_interval'],
        experiment_name='omnibot_nav',
        run_name='',
        logger='tensorboard',
        neptune_project='omnibot/nav-rl',
        wandb_project='omnibot_nav',
        resume=args.resume is not None,
        load_run=args.resume or '',
        load_checkpoint='',
        log_dir=os.path.expanduser(args.log_dir),
    )

    # ── PPO hyperparameters ────────────────────────────────────────────────
    from rsl_rl.algorithms import PPO
    from rsl_rl.modules import ActorCritic

    actor_critic_cfg = {
        'class_name': 'ActorCritic',
        'actor_hidden_dims':  net_cfg['actor_hidden_dims'],
        'critic_hidden_dims': net_cfg['critic_hidden_dims'],
        'activation':         net_cfg['activation'],
        'init_noise_std':     net_cfg['init_noise_std'],
    }
    alg_cfg = {
        'class_name': 'PPO',
        'value_loss_coef':       ppo_cfg['value_loss_coef'],
        'use_clipped_value_loss': ppo_cfg['use_clipped_value_loss'],
        'clip_param':            ppo_cfg['clip_param'],
        'entropy_coef':          ppo_cfg['entropy_coef'],
        'num_learning_epochs':   ppo_cfg['num_learning_epochs'],
        'num_mini_batches':      ppo_cfg['num_mini_batches'],
        'learning_rate':         ppo_cfg['learning_rate'],
        'schedule':              ppo_cfg['schedule'],
        'gamma':                 ppo_cfg['gamma'],
        'lam':                   ppo_cfg['lam'],
        'desired_kl':            ppo_cfg['desired_kl'],
        'max_grad_norm':         ppo_cfg['max_grad_norm'],
    }

    runner = OnPolicyRunner(
        env,
        {'runner': runner_cfg.__dict__, 'policy': actor_critic_cfg, 'algorithm': alg_cfg},
        log_dir=os.path.expanduser(args.log_dir),
        device='cuda' if torch.cuda.is_available() else 'cpu',
    )

    if args.resume:
        runner.load(args.resume)

    print(f'Starting navigation policy training: {args.max_iterations} iterations, '
          f'{args.num_envs} environments.')
    runner.learn(num_learning_iterations=args.max_iterations, init_at_random_ep_len=True)

    env.close()
    simulation_app.close()


if __name__ == '__main__':
    main()
