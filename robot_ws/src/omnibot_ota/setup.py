from setuptools import setup
import os
from glob import glob

package_name = "omnibot_ota"

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
    description="OTA update agent for OmniBot",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "ota_agent = omnibot_ota.ota_agent_node:main",
        ],
    },
)
