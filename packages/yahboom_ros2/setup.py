from setuptools import setup, find_packages
import os
from glob import glob

package_name = "yahboom_ros2"

setup(
    name=package_name,
    version="1.0.0",
    packages=find_packages(exclude=["test"]),
    data_files=[
        ("share/ament_index/resource_index/packages", ["resource/" + package_name]),
        ("share/" + package_name, ["package.xml"]),
        (os.path.join("share", package_name, "launch"), glob("launch/*.launch.py")),
        (os.path.join("share", package_name, "config"), glob("config/*.yaml")),
    ],
    install_requires=["setuptools", "pyserial", "numpy"],
    zip_safe=True,
    maintainer="Varun Vaidhiya",
    maintainer_email="varunvaidhiya@example.com",
    description="ROS 2 driver for the Yahboom ROS Robot Expansion Board",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "yahboom_driver = yahboom_ros2.driver_node:main",
        ],
    },
)
