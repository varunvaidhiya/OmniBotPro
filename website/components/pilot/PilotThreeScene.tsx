"use client";

/*
 * PilotThreeScene — renders the Three.js Physics provider's scene state
 * as a live 3-D view using @react-three/fiber and @react-three/drei.
 *
 * Mounts alongside the SVG camera view. When the provider has
 * getSceneObjects(), this component renders the robot, obstacles, and
 * ground plane in real time at 20 Hz.
 */

import { Canvas } from "@react-three/fiber";
import { OrbitControls, Grid, ContactShadows } from "@react-three/drei";
import { useEffect, useRef, useMemo } from "react";
import * as THREE from "three";
import type { ThreeSceneState } from "@/lib/pilot/sim/types";

interface Props {
  sceneState: ThreeSceneState | null;
  width?: number;
  height?: number;
}

export default function PilotThreeScene({ sceneState, width = 560, height = 320 }: Props) {
  return (
    <Canvas
      shadows
      dpr={[1, 2]}
      gl={{ antialias: true, alpha: false, toneMapping: THREE.ACESFilmicToneMapping }}
      camera={{ position: [0, 3, 2], fov: 50, near: 0.05, far: 20 }}
      style={{ width, height, background: "#0A1322" }}
    >
      <ambientLight intensity={0.4} />
      <directionalLight
        position={[5, 8, 3]}
        intensity={1.8}
        castShadow
        shadow-mapSize={[512, 512]}
      />
      <hemisphereLight args={["#b1e1ff", "#0A1322", 0.4]} />
      <Grid
        args={[10, 10]}
        cellSize={0.5}
        cellThickness={0.5}
        cellColor="#1a3050"
        sectionSize={2}
        sectionThickness={1}
        sectionColor="#0a6f8c"
        fadeDistance={6}
        infiniteGrid
        position={[0, 0.001, 0]}
      />
      <ContactShadows position={[0, 0, 0]} opacity={0.4} scale={8} blur={2} far={2} />
      {sceneState ? (
        <SceneContent state={sceneState} />
      ) : (
        <mesh position={[0, 0.5, 0]}>
          <boxGeometry args={[0.3, 0.3, 0.3]} />
          <meshStandardMaterial color="#00D4FF" wireframe />
        </mesh>
      )}
      <OrbitControls
        enableDamping
        dampingFactor={0.08}
        target={[0, 0.1, 0]}
        maxPolarAngle={1.3}
        minDistance={0.8}
        maxDistance={6}
      />
    </Canvas>
  );
}

function SceneContent({ state }: { state: ThreeSceneState }) {
  const { robot, obstacles, armJoints } = state;

  return (
    <group>
      {/* Ground plane */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.01, 0]} receiveShadow>
        <planeGeometry args={[10, 10]} />
        <meshStandardMaterial color="#0A1322" />
      </mesh>

      {/* Robot base */}
      <group
        position={[robot.x, robot.height / 2, robot.y]}
        rotation={[0, robot.theta, 0]}
      >
        {/* Chassis */}
        <mesh castShadow>
          <boxGeometry args={[robot.length, robot.height, robot.width]} />
          <meshStandardMaterial color="#12151c" metalness={0.6} roughness={0.4} />
        </mesh>

        {/* Accent strip */}
        <mesh position={[robot.length / 2, 0, 0]}>
          <boxGeometry args={[0.01, 0.01, robot.width * 0.5]} />
          <meshStandardMaterial color="#00D4FF" emissive="#00D4FF" emissiveIntensity={0.8} />
        </mesh>

        {/* Wheels */}
        {[[1, 1], [1, -1], [-1, 1], [-1, -1]].map(([fx, fz], i) => (
          <mesh
            key={i}
            position={[
              fx * (robot.length / 2 - 0.03),
              -robot.height / 2 + 0.04,
              fz * (robot.width / 2 + 0.01),
            ]}
            rotation={[Math.PI / 2, 0, 0]}
            castShadow
          >
            <cylinderGeometry args={[0.04, 0.04, 0.032, 16]} />
            <meshStandardMaterial color="#0a0a0c" metalness={0.1} roughness={0.85} />
          </mesh>
        ))}

        {/* Arm (simplified 6-DOF) */}
        {armJoints.length >= 6 && <ArmRig joints={armJoints} length={robot.length} height={robot.height} />}
      </group>

      {/* Obstacles */}
      {obstacles.map((obs, i) => (
        <mesh key={i} position={[obs.x, obs.h / 2, obs.y]} castShadow receiveShadow>
          <boxGeometry args={[obs.w, obs.h, obs.w]} />
          <meshStandardMaterial
            color={obs.color}
            metalness={0.3}
            roughness={0.5}
            transparent
            opacity={0.6}
          />
        </mesh>
      ))}
    </group>
  );
}

function ArmRig({ joints, length, height }: { joints: number[]; length: number; height: number }) {
  const [pan, lift, elbow, wristFlex, wristRoll, grip] = joints;
  const armBaseY = height / 2;
  const segLen = 0.15;

  return (
    <group position={[length * 0.12, armBaseY, 0]}>
      {/* Pedestal */}
      <mesh>
        <boxGeometry args={[0.06, 0.04, 0.05]} />
        <meshStandardMaterial color="#d4d7db" metalness={0.1} roughness={0.6} />
      </mesh>

      {/* Shoulder pan + lift */}
      <group position={[0, 0.03, 0]} rotation={[0, pan, 0]}>
        <mesh>
          <cylinderGeometry args={[0.018, 0.018, 0.04, 16]} />
          <meshStandardMaterial color="#0d0f12" metalness={0.3} roughness={0.45} />
        </mesh>
        <group position={[0, 0.02, 0]} rotation={[-lift, 0, 0]}>
          <mesh position={[0, segLen / 2, 0]}>
            <boxGeometry args={[0.035, segLen, 0.035]} />
            <meshStandardMaterial color="#d4d7db" metalness={0.1} roughness={0.6} />
          </mesh>
          {/* Elbow */}
          <group position={[0, segLen, 0]} rotation={[0, 0, elbow]}>
            <mesh>
              <cylinderGeometry args={[0.016, 0.016, 0.035, 16]} />
              <meshStandardMaterial color="#0d0f12" metalness={0.3} roughness={0.45} />
            </mesh>
            <group position={[0, segLen / 2, 0]}>
              <mesh>
                <boxGeometry args={[0.03, segLen, 0.03]} />
                <meshStandardMaterial color="#d4d7db" metalness={0.1} roughness={0.6} />
              </mesh>
              {/* Wrist flex */}
              <group position={[0, segLen, 0]} rotation={[wristFlex, 0, 0]}>
                <mesh>
                  <cylinderGeometry args={[0.012, 0.012, 0.03, 12]} />
                  <meshStandardMaterial color="#0d0f12" />
                </mesh>
                {/* Gripper */}
                <group position={[0, 0.025, 0]} rotation={[0, 0, wristRoll]}>
                  <mesh>
                    <boxGeometry args={[0.04, 0.03, 0.04]} />
                    <meshStandardMaterial color="#d4d7db" />
                  </mesh>
                  {[-1, 1].map((s) => (
                    <mesh key={s} position={[0, 0.025, s * (0.01 + 0.015 * grip)]}>
                      <boxGeometry args={[0.01, 0.03, 0.01]} />
                      <meshStandardMaterial color="#d4d7db" />
                    </mesh>
                  ))}
                </group>
              </group>
            </group>
          </group>
        </group>
      </group>
    </group>
  );
}
