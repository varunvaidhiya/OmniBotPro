#!/usr/bin/env python3
"""
urdf_to_glb.py  —  Convert OmniBot URDF + STL meshes to a single robot.glb
                    for use with the Android SceneView 3D Robot Viewer.

Requirements:
    pip install trimesh[easy] numpy lxml pygltflib

Usage:
    python tools/urdf_to_glb.py
    # Output: android_app/app/src/main/assets/robot.glb

The script reads omnibot.urdf.xacro (after xacro expansion), loads all
referenced STL meshes, applies the joint origin transforms from the URDF,
and exports a single GLB where each link is a named glTF node.

Node naming matches the URDF link names exactly so RobotViewerFragment can
find and animate them at runtime from /joint_states + /arm/joint_states.
"""

import os
import sys
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
import numpy as np

# ── Resolve repo root ──────────────────────────────────────────────────────────
SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent
URDF_XACRO = REPO_ROOT / "robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro"
MESH_DIR = REPO_ROOT / "robot_ws/src/omnibot_description/meshes"
OUTPUT_GLB = REPO_ROOT / "android_app/app/src/main/assets/robot.glb"

# ── Colours for the procedural fallback geometry ──────────────────────────────
COLOUR_BASE = [0.25, 0.25, 0.30, 1.0]  # dark grey  — robot body
COLOUR_ARM = [0.15, 0.50, 0.80, 1.0]  # blue       — arm links
COLOUR_WHEEL = [0.10, 0.10, 0.10, 1.0]  # black      — wheels
COLOUR_FLOOR = [0.06, 0.06, 0.10, 0.6]  # dark blue transparent — floor


def run_xacro(xacro_path: Path) -> Path:
    """Expand xacro → plain URDF.  Returns path to temp file."""
    tmp = tempfile.NamedTemporaryFile(suffix=".urdf", delete=False)
    result = subprocess.run(["xacro", str(xacro_path)], capture_output=True, text=True)
    if result.returncode != 0:
        # xacro not installed — fall back to reading it as plain XML (may fail on macros)
        print(
            "  [warn] xacro not found, attempting plain XML parse (macros may not expand)"
        )
        tmp.write(xacro_path.read_bytes())
    else:
        tmp.write(result.stdout.encode())
    tmp.close()
    return Path(tmp.name)


def parse_origin(origin_elem) -> tuple[np.ndarray, np.ndarray]:
    """Parse a URDF <origin> element into (xyz, rpy) numpy arrays."""
    if origin_elem is None:
        return np.zeros(3), np.zeros(3)
    xyz_str = origin_elem.get("xyz", "0 0 0")
    rpy_str = origin_elem.get("rpy", "0 0 0")
    return (
        np.array([float(v) for v in xyz_str.split()]),
        np.array([float(v) for v in rpy_str.split()]),
    )


def rpy_to_matrix(rpy: np.ndarray) -> np.ndarray:
    """Roll-Pitch-Yaw → 4×4 rotation matrix (intrinsic XYZ)."""
    r, p, y = rpy
    cr, sr = np.cos(r), np.sin(r)
    cp, sp = np.cos(p), np.sin(p)
    cy, sy = np.cos(y), np.sin(y)
    Rx = np.array([[1, 0, 0], [0, cr, -sr], [0, sr, cr]])
    Ry = np.array([[cp, 0, sp], [0, 1, 0], [-sp, 0, cp]])
    Rz = np.array([[cy, -sy, 0], [sy, cy, 0], [0, 0, 1]])
    R = Rz @ Ry @ Rx
    T = np.eye(4)
    T[:3, :3] = R
    return T


def origin_to_transform(origin_elem) -> np.ndarray:
    xyz, rpy = parse_origin(origin_elem)
    T = rpy_to_matrix(rpy)
    T[:3, 3] = xyz
    return T


def load_stl(stl_path: Path, colour: list[float]):
    """Load STL, return trimesh.Trimesh with vertex colours applied."""
    import trimesh

    mesh = trimesh.load(str(stl_path), force="mesh")
    mesh.visual = trimesh.visual.ColorVisuals(
        mesh=mesh,
        vertex_colors=np.tile(
            np.array([[int(c * 255) for c in colour]], dtype=np.uint8),
            (len(mesh.vertices), 1),
        ),
    )
    return mesh


def build_scene(urdf_path: Path):
    """Parse URDF and build a trimesh.Scene with one node per link."""
    import trimesh

    tree = ET.parse(urdf_path)
    root = tree.getroot()

    # ── Collect links ──────────────────────────────────────────────────────────
    link_meshes: dict[str, "trimesh.Trimesh | None"] = {}
    for link in root.iter("link"):
        name = link.get("name", "")
        visual = link.find("visual")
        if visual is None:
            link_meshes[name] = None
            continue

        geometry = visual.find("geometry")
        mesh_elem = geometry.find("mesh") if geometry is not None else None

        if mesh_elem is None:
            # Primitive geometry (box / cylinder / sphere) — create procedural mesh
            box = geometry.find("box") if geometry is not None else None
            if box is not None:
                size = [float(v) for v in box.get("size", "0.1 0.1 0.1").split()]
                colour = COLOUR_BASE if "base" in name else COLOUR_ARM
                tm = trimesh.creation.box(extents=size)
                tm.visual = trimesh.visual.ColorVisuals(
                    mesh=tm,
                    vertex_colors=np.tile(
                        np.array([[int(c * 255) for c in colour]], dtype=np.uint8),
                        (len(tm.vertices), 1),
                    ),
                )
                link_meshes[name] = tm
                continue

            cylinder = geometry.find("cylinder") if geometry is not None else None
            if cylinder is not None:
                r = float(cylinder.get("radius", "0.02"))
                h = float(cylinder.get("length", "0.05"))
                colour = COLOUR_WHEEL if "wheel" in name else COLOUR_ARM
                tm = trimesh.creation.cylinder(radius=r, height=h)
                tm.visual = trimesh.visual.ColorVisuals(
                    mesh=tm,
                    vertex_colors=np.tile(
                        np.array([[int(c * 255) for c in colour]], dtype=np.uint8),
                        (len(tm.vertices), 1),
                    ),
                )
                link_meshes[name] = tm
                continue

            link_meshes[name] = None
            continue

        # Resolve STL path (package:// URI)
        filename = mesh_elem.get("filename", "")
        filename = filename.replace(
            "package://omnibot_description/",
            str(REPO_ROOT / "robot_ws/src/omnibot_description") + "/",
        )

        stl_path = Path(filename)
        if not stl_path.exists():
            # Try relative to MESH_DIR
            stl_path = MESH_DIR / stl_path.name

        if not stl_path.exists():
            print(f"  [warn] mesh not found: {filename}")
            link_meshes[name] = None
            continue

        colour = (
            COLOUR_WHEEL
            if "wheel" in name
            else (COLOUR_BASE if "base" in name else COLOUR_ARM)
        )
        # Scale from mm→m if the mesh looks too large
        tm = load_stl(stl_path, colour)
        scale_str = mesh_elem.get("scale", "1 1 1")
        scale = [float(v) for v in scale_str.split()]
        if scale != [1.0, 1.0, 1.0]:
            tm.apply_scale(scale)

        # Apply visual origin transform
        vis_origin = visual.find("origin")
        tm.apply_transform(origin_to_transform(vis_origin))

        link_meshes[name] = tm

    # ── Collect joints → build transform tree ────────────────────────────────
    joint_transforms: dict[str, np.ndarray] = {}
    parent_map: dict[str, str] = {}  # child_link → parent_link

    for joint in root.iter("joint"):
        parent_elem = joint.find("parent")
        child_elem = joint.find("child")
        if parent_elem is None or child_elem is None:
            continue
        parent_link = parent_elem.get("link", "")
        child_link = child_elem.get("link", "")
        origin = joint.find("origin")
        T = origin_to_transform(origin)
        joint_transforms[child_link] = T
        parent_map[child_link] = parent_link

    # ── Compute world transforms via DFS ────────────────────────────────────
    def world_transform(link_name: str) -> np.ndarray:
        if link_name not in parent_map:
            return np.eye(4)  # root link
        parent = parent_map[link_name]
        T_parent = world_transform(parent)
        T_joint = joint_transforms.get(link_name, np.eye(4))
        return T_parent @ T_joint

    # ── Build scene ───────────────────────────────────────────────────────────
    scene = trimesh.scene.Scene()

    for link_name, mesh in link_meshes.items():
        if mesh is None:
            continue
        T = world_transform(link_name)
        transformed = mesh.copy()
        transformed.apply_transform(T)
        scene.add_geometry(transformed, node_name=link_name)

    # ── Add floor grid (1 m × 1 m, 0.1 m cells) ────────────────────────────
    grid_lines = []
    for i in range(-5, 6):
        v = i * 0.1
        grid_lines += [[v, 0.0, -0.5], [v, 0.0, 0.5]]
        grid_lines += [[-0.5, 0.0, v], [0.5, 0.0, v]]
    # Export grid as a thin plane instead of lines (trimesh GLB doesn't support lines well)
    floor = trimesh.creation.box(extents=[2.0, 0.002, 2.0])
    floor.visual = trimesh.visual.ColorVisuals(
        mesh=floor,
        vertex_colors=np.tile(
            np.array([[15, 15, 25, 120]], dtype=np.uint8), (len(floor.vertices), 1)
        ),
    )
    floor.apply_translation([0, -0.001, 0])
    scene.add_geometry(floor, node_name="floor_plane")

    return scene


def main():
    print("OmniBot URDF → GLB exporter")
    print(f"  URDF:   {URDF_XACRO}")
    print(f"  Output: {OUTPUT_GLB}")

    try:
        import trimesh  # noqa: F401
    except ImportError:
        print(
            "\n[error] trimesh not installed.  Run:\n  pip install trimesh[easy] numpy lxml"
        )
        sys.exit(1)

    if not URDF_XACRO.exists():
        print(f"\n[error] URDF not found at {URDF_XACRO}")
        sys.exit(1)

    print("\nStep 1/3  Expanding xacro…")
    urdf_tmp = run_xacro(URDF_XACRO)

    print("Step 2/3  Building scene from URDF + STL meshes…")
    scene = build_scene(urdf_tmp)
    os.unlink(urdf_tmp)

    geom_count = len(list(scene.geometry.keys()))
    print(f"          {geom_count} geometries loaded")

    print("Step 3/3  Exporting to GLB…")
    OUTPUT_GLB.parent.mkdir(parents=True, exist_ok=True)
    glb_bytes = scene.export(file_type="glb")
    OUTPUT_GLB.write_bytes(glb_bytes)
    size_kb = OUTPUT_GLB.stat().st_size // 1024
    print(f"\n✓  Exported {size_kb} KB → {OUTPUT_GLB}")
    print("\nNext steps:")
    print("  1. Rebuild the Android app so assets/ is re-packaged")
    print("  2. Open the Map → 3D Robot tab to see the live animated model")
    print("\nNode names in the GLB match URDF link names, e.g.:")
    print("  arm_shoulder_pan, arm_shoulder_lift, arm_elbow_flex, …")
    print("  RobotViewerFragment will animate these from /arm/joint_states")


if __name__ == "__main__":
    main()
