from setuptools import find_packages, setup

package_name = "omnibot_metrics"

setup(
    name=package_name,
    version="0.1.0",
    packages=find_packages(exclude=["test"]),
    data_files=[
        ("share/ament_index/resource_index/packages", ["resource/" + package_name]),
        ("share/" + package_name, ["package.xml"]),
        ("share/" + package_name + "/launch", ["launch/metrics.launch.py"]),
        ("share/" + package_name + "/config", ["config/metrics_params.yaml"]),
    ],
    install_requires=["setuptools"],
    zip_safe=True,
    maintainer="Varun Vaidhiya",
    maintainer_email="varun.vaidhiya@gmail.com",
    description="Prometheus metrics bridge for OmniBot",
    license="Apache-2.0",
    tests_require=["pytest"],
    entry_points={
        "console_scripts": [
            "metrics_bridge = omnibot_metrics.ros2_prometheus_bridge:main",
        ],
    },
)
