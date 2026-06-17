^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Changelog for package ros2_bev_stitcher
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

1.1.0 (2026-06-17)
-------------------
* OhhO View — geometric IPM + calibration UI
* ``bev_ipm.py``: geometric inverse-perspective-mapping calibrator

  * Computes per-camera homographies from camera extrinsic params
    (position + orientation in base_link) and intrinsic params (K matrix)
  * Uses pinhole camera geometry with a known ground plane (z=0)
  * No checkerboard needed — camera poses can come from URDF, TF2, or params
  * ``camera_rotation(yaw, pitch, roll)``: intuitive camera orientation from
    look direction (yaw), vertical tilt (pitch), and twist (roll)
  * CLI: ``ros2 run ros2_bev_stitcher bev_ipm --help``
  * ROS params: per-camera position, orientation_rpy, fx/fy/cx/cy
  * Produces .npz file compatible with existing stitcher node

* ``bev_cal_ui.py``: interactive calibration wizard

  * Live BEV preview with OpenCV trackbars for each camera's 6-DOF pose
  * Camera selector trackbar to switch between cameras
  * Real-time adjustment of position (x,y,z), orientation (yaw,pitch,roll),
    intrinsics (fx,fy,cx,cy), canvas scale (px/m), and ground height
  * Save to .npz + human-readable params reference file
  * CLI: ``ros2 run ros2_bev_stitcher bev_cal_ui --help``

* ``bev_stitcher_node.py``: calibration_mode parameter

  * ``checkerboard``: load homographies from .npz file (existing behaviour)
  * ``ipm``: compute homographies geometrically from camera pose params
  * ``auto``: try IPM first, fall back to .npz, then tiled grid
  * Per-camera IPM params auto-declared with sensible defaults
  * New params: ``calibration_mode``, ``pixels_per_meter``, ``ground_z``,
    ``<camera>.position``, ``<camera>.orientation_rpy``,
    ``<camera>.fx/fy/cx/cy``

* ``bev_stitcher.launch.py``: additional launch args for IPM mode
* ``config/bev_params.yaml``: added IPM parameters with documented defaults
* ``README.md``: complete rewrite with IPM docs, UI guide, and usage examples

1.0.0 (2026-03-15)
-------------------
* Initial release: multi-camera Bird's Eye View compositor for ROS 2
* Extracted and generalized from the OmniBot omnibot_lerobot package
* ``bev_stitcher_node.py``: subscribes to N perspective cameras, publishes
  a single top-down BEV image

  * Camera list fully configurable via ``camera_names`` ROS 2 parameter
    (not hardcoded to front/rear/left/right)
  * Topic pattern configurable: ``input_topic_pattern`` (default
    ``/camera/{name}/image_raw``)
  * Canvas size, output resolution, publish rate all parameterized
  * Per-camera homography warp via OpenCV ``warpPerspective``
  * Distance-weighted alpha blending in overlap regions
  * Graceful fallback to tiled 2×N grid when calibration file absent
  * ``calibration_file`` ROS 2 parameter (default ``~/bev_calibration.npz``)

* ``bev_calibrate.py``: checkerboard-based one-time calibration tool

  * Subscribes to all camera topics, captures one frame each
  * Detects inner corners of a flat checkerboard (configurable pattern size
    and square size in metres)
  * Computes RANSAC homography per camera to shared BEV ground plane
  * Shows warped preview per camera before saving
  * Saves all homographies to ``.npz`` file loaded by the stitcher node
  * CLI: ``ros2 run ros2_bev_stitcher bev_calibrate --help``

* Launch file: ``bev_stitcher.launch.py``
* Param YAML: ``config/bev_params.yaml``
* Contributors: Varun Vaidhiya
