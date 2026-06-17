/*
 * Mock sensor configuration and 3D simulation for OhhO View.
 */

import { useState, useCallback } from "react";

export interface SensorLayer {
  id: string;
  name: string;
  topic: string;
  type: "pointcloud" | "image" | "map" | "tf" | "laserscan";
  visible: boolean;
  hz: number;
}

const INITIAL_LAYERS: SensorLayer[] = [
  { id: "s1", name: "3D Wrist Depth", topic: "/camera/wrist/depth/points", type: "pointcloud", visible: true, hz: 30 },
  { id: "s2", name: "2D Lidar", topic: "/scan", type: "laserscan", visible: true, hz: 15 },
  { id: "s3", name: "Occupancy Map", topic: "/map", type: "map", visible: true, hz: 1 },
  { id: "s4", name: "Robot Transforms", topic: "/tf", type: "tf", visible: true, hz: 100 },
  { id: "s5", name: "RGB Front Camera", topic: "/camera/front/image_raw", type: "image", visible: false, hz: 30 },
  { id: "s6", name: "BEV Stitcher", topic: "/camera/base/bev/image_raw", type: "image", visible: false, hz: 10 },
];

export function useSensors() {
  const [layers, setLayers] = useState<SensorLayer[]>(INITIAL_LAYERS);

  const toggleLayer = useCallback((id: string) => {
    setLayers((prev) =>
      prev.map((l) => (l.id === id ? { ...l, visible: !l.visible } : l))
    );
  }, []);

  return { layers, toggleLayer };
}
