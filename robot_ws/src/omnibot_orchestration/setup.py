from setuptools import setup
import os
from glob import glob

package_name = "omnibot_orchestration"

setup(
    name=package_name,
    version="0.1.0",
    packages=[
        package_name,
        package_name + ".tools",
        package_name + ".memory",
        package_name + ".agent",
        package_name + ".vision",
        package_name + ".graph",
        package_name + ".planning",
        package_name + ".generation",
    ],
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
    description="LangChain ReAct agent layer for natural-language robot orchestration on OmniBot",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "langchain_agent_node = omnibot_orchestration.langchain_agent_node:main",
            "world_state_node = omnibot_orchestration.world_state_node:main",
            "agent_node = omnibot_orchestration.agent_node:main",
        ],
    },
)
