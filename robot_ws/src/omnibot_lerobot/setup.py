from setuptools import find_packages, setup
import os
from glob import glob

package_name = "omnibot_lerobot"

setup(
    name=package_name,
    version="0.1.0",
    packages=find_packages(exclude=["test"]),
    data_files=[
        ("share/ament_index/resource_index/packages", ["resource/" + package_name]),
        ("share/" + package_name, ["package.xml"]),
        (
            os.path.join("share", package_name, "launch"),
            glob(os.path.join("launch", "*.launch.py")),
        ),
        (
            os.path.join("share", package_name, "config"),
            glob(os.path.join("config", "*.yaml")),
        ),
        (
            os.path.join("lib", package_name),
            glob(os.path.join("omnibot_lerobot", "*_node.py")),
        ),
    ],
    install_requires=["setuptools"],
    zip_safe=True,
    maintainer="varunvaidhiya",
    maintainer_email="varunvaidhiya@todo.todo",
    description="Model-agnostic visuomotor policy node for OmniBot mobile manipulation",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "policy_node = omnibot_lerobot.policy_node:main",
            "teleop_recorder_node = omnibot_lerobot.teleop_recorder_node:main",
            "bev_stitcher_node = omnibot_lerobot.bev_stitcher_node:main",
        ],
    },
)
