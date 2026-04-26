from setuptools import setup
import os
from glob import glob

package_name = "omnibot_vla"

setup(
    name=package_name,
    version="0.0.1",
    packages=[package_name],
    data_files=[
        ("share/ament_index/resource_index/packages", ["resource/" + package_name]),
        ("share/" + package_name, ["package.xml"]),
        (os.path.join("share", package_name, "launch"), glob("launch/*.launch.py")),
    ],
    install_requires=["setuptools"],
    zip_safe=True,
    maintainer="varunvaidhiya",
    maintainer_email="varunvaidhiya@todo.todo",
    description="OpenVLA integration for OmniBot control",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": ["vla_node = omnibot_vla.vla_node:main"],
    },
)
