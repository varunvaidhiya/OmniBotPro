from setuptools import setup
import os
from glob import glob

package_name = "omnibot_vr"

setup(
    name=package_name,
    version="0.1.0",
    packages=[package_name],
    data_files=[
        # Required by ament/colcon
        ("share/ament_index/resource_index/packages", ["resource/" + package_name]),
        ("share/" + package_name, ["package.xml"]),
        # Launch files
        (os.path.join("share", package_name, "launch"), glob("launch/*.launch.py")),
    ],
    install_requires=["setuptools"],
    zip_safe=True,
    maintainer="OmniBot Team",
    maintainer_email="omnibot@example.com",
    description="VR control bridge for OmniBot",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "vr_recording_bridge = omnibot_vr.vr_recording_bridge:main",
        ],
    },
)
