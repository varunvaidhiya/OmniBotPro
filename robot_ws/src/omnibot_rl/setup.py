from setuptools import setup
import os
from glob import glob

package_name = 'omnibot_rl'

setup(
    name=package_name,
    version='1.0.0',
    packages=[package_name],
    data_files=[
        ('share/ament_index/resource_index/packages',
            ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
        (os.path.join('share', package_name, 'launch'), glob('launch/*.launch.py')),
        (os.path.join('share', package_name, 'config'), glob('config/*.yaml')),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='varunvaidhiya',
    maintainer_email='varunvaidhiya@todo.todo',
    description='Isaac Lab RL inference nodes for OmniBot sim-to-real deployment',
    license='Apache-2.0',
    tests_require=['pytest'],
    entry_points={
        'console_scripts': [
            'rl_nav_node        = omnibot_rl.rl_nav_node:main',
            'rl_arm_node        = omnibot_rl.rl_arm_node:main',
            'arm_cmd_mux        = omnibot_rl.arm_cmd_mux:main',
            'rl_object_pose_node = omnibot_rl.rl_object_pose_node:main',
        ],
    },
)
