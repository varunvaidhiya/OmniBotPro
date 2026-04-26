from setuptools import setup, find_packages
import os
from glob import glob

package_name = "ros2_bev_stitcher"

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
    install_requires=["setuptools", "numpy", "opencv-python"],
    zip_safe=True,
    maintainer="Varun Vaidhiya",
    maintainer_email="varunvaidhiya@example.com",
    description="Multi-camera Bird's Eye View compositor for ROS 2",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "bev_stitcher = ros2_bev_stitcher.bev_stitcher_node:main",
            "bev_calibrate = ros2_bev_stitcher.bev_calibrate:main",
        ],
    },
)
