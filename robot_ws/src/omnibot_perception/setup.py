from setuptools import find_packages, setup
import os
from glob import glob

package_name = 'omnibot_perception'

setup(
    name=package_name,
    version='0.1.0',
    packages=find_packages(exclude=['test']),
    data_files=[
        ('share/ament_index/resource_index/packages',
         ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
        (os.path.join('share', package_name, 'config'),
         glob('config/*.yaml')),
        (os.path.join('share', package_name, 'launch'),
         glob('launch/*.py')),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='OmniBot Team',
    maintainer_email='varun.vaidhiya@gmail.com',
    description='Unified perception pipeline: detection, tracking, segmentation, distance fusion',
    license='Apache-2.0',
    tests_require=['pytest'],
    entry_points={
        'console_scripts': [
            'perception_node = omnibot_perception.perception_node:main',
            'distance_estimator_node = omnibot_perception.distance_estimator_node:main',
            'vla_trigger_node = omnibot_perception.vla_trigger_node:main',
            'ultrasonic_driver_node = omnibot_perception.ultrasonic_driver_node:main',
        ],
    },
)
