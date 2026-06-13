from setuptools import setup
import os
from glob import glob

package_name = "omnibot_perception"

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
    maintainer_email="varun.vaidhiya@gmail.com",
    description="Modular AI perception: object distance + pose estimation from cameras",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "object_perception_node = omnibot_perception.object_perception_node:main",
        ],
    },
)
