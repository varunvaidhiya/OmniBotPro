from setuptools import setup, find_packages

setup(
    name="vla_data_engine",
    version="0.1.0",
    packages=find_packages(),
    install_requires=[
        "numpy>=1.24.0",
        "opencv-python>=4.8.0",
        "rosbags>=0.9.16",
        "torch>=2.1.0",
        "torchvision>=0.16.0",
        "pandas>=2.0.0",
        "pyarrow>=14.0.0",
        "pillow>=10.0.0",
        "tqdm>=4.66.0",
        "pyyaml>=6.0",
        "click>=8.1.0",
    ],
)
