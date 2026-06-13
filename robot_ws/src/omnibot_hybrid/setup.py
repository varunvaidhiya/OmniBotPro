from setuptools import setup
import os
from glob import glob

package_name = "omnibot_hybrid"

setup(
    name=package_name,
    version="1.0.0",
    packages=[package_name],
    data_files=[
        ("share/ament_index/resource_index/packages", ["resource/" + package_name]),
        ("share/" + package_name, ["package.xml"]),
        (os.path.join("share", package_name, "launch"), glob("launch/*.launch.py")),
        (os.path.join("share", package_name, "config"), glob("config/*.yaml")),
    ],
    install_requires=["setuptools"],
    zip_safe=True,
    maintainer="varunvaidhiya",
    maintainer_email="varunvaidhiya@todo.todo",
    description="Hybrid Nav2 + VLA control for OmniBot",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "cmd_vel_mux = omnibot_hybrid.cmd_vel_mux:main",
            "mission_planner = omnibot_hybrid.mission_planner:main",
            "rosbag_recorder = omnibot_hybrid.rosbag_recorder:main",
        ],
    },
)
