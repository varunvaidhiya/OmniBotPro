/*
 * Sensor configuration for OhhO View — derived from the selected robot.
 *
 * Was a fixed OmniBot list; now `layersFor(config)` builds the sensor tree from
 * the robot's RobotConfig so a drone shows FPV/down cameras + GPS, a fixed arm
 * shows only a wrist camera (no map), a quadruped shows 3D lidar, etc.
 */

import { useState, useCallback, useEffect } from "react";

import type { RobotConfig, SensorKind } from "@/lib/garage/robot-config";

export interface SensorLayer {
  id: string;
  name: string;
  topic: string;
  type: "pointcloud" | "image" | "map" | "tf" | "laserscan";
  visible: boolean;
  hz: number;
}

/** Map a structured SensorKind onto a View layer type (or null if not visualized). */
function layerType(kind: SensorKind): SensorLayer["type"] | null {
  switch (kind) {
    case "rgb_camera":
    case "stereo_camera":
    case "thermal_camera":
    case "fpv_camera":
    case "bev":
      return "image";
    case "depth_camera":
    case "lidar_3d":
      return "pointcloud";
    case "lidar_2d":
      return "laserscan";
    default:
      return null; // imu / gps / sonar / baro / encoder / force-torque aren't 3D layers
  }
}

/** Build the View sensor tree for a robot. */
export function layersFor(config: RobotConfig): SensorLayer[] {
  const layers: SensorLayer[] = [];
  let i = 0;

  // Mobile robots localize against a map; a fixed arm does not.
  if (config.capabilities.canNavigate) {
    layers.push({ id: `s${i++}`, name: "Occupancy Map", topic: "/map", type: "map", visible: true, hz: 1 });
  }
  layers.push({ id: `s${i++}`, name: "Robot Transforms", topic: "/tf", type: "tf", visible: true, hz: 100 });

  for (const s of config.sensors) {
    const t = layerType(s.kind);
    if (!t) continue;
    layers.push({ id: `s${i++}`, name: s.label, topic: s.topic, type: t, visible: layers.length < 4, hz: s.hz });
  }
  return layers;
}

export function useSensors(config: RobotConfig) {
  const [layers, setLayers] = useState<SensorLayer[]>(() => layersFor(config));

  // Rebuild when the selected robot changes.
  useEffect(() => {
    setLayers(layersFor(config));
  }, [config]);

  const toggleLayer = useCallback((id: string) => {
    setLayers((prev) => prev.map((l) => (l.id === id ? { ...l, visible: !l.visible } : l)));
  }, []);

  return { layers, toggleLayer };
}
