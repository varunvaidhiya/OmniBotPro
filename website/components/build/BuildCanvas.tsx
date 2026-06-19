"use client";

/*
 * BuildCanvas — a live, parametric Three.js view of the robot the user is
 * designing. It reads the Design's selected parts straight from the catalog and
 * assembles a chassis + wheels + battery + compute + arm + gripper + sensors,
 * sized from each part's real dimensions. The category the user is editing
 * glows in the brand accent so they can see what they're touching.
 *
 * Deliberately lighter than the homepage's OmniBotModel (no postprocessing) so
 * it stays smooth while the user drags requirement sliders and swaps parts.
 */

import { Canvas } from "@react-three/fiber";
import {
  ContactShadows,
  Environment,
  Grid,
  Lightformer,
  OrbitControls,
  RoundedBox,
} from "@react-three/drei";
import { Suspense, useMemo } from "react";
import * as THREE from "three";

import { getPart, type Category, type Part } from "@/lib/build/catalog";
import { selectedIds, singleSelected, type Design } from "@/lib/build/design";

const CYAN = "#00D4FF";

function useMaterials(highlight: Category | null) {
  return useMemo(() => {
    const make = (c: string, metalness: number, roughness: number) =>
      new THREE.MeshStandardMaterial({ color: c, metalness, roughness, envMapIntensity: 1 });
    const accent = new THREE.MeshStandardMaterial({
      color: CYAN,
      emissive: new THREE.Color(CYAN),
      emissiveIntensity: 1.4,
      metalness: 0.3,
      roughness: 0.4,
    });
    return {
      highlight,
      accent,
      chassis: make("#12151c", 0.6, 0.42),
      arm: make("#d4d7db", 0.0, 0.62),
      servo: make("#0d0f12", 0.3, 0.45),
      tire: make("#0a0a0c", 0.1, 0.85),
      hub: make("#cfd6dd", 1.0, 0.18),
      battery: make("#1c2a1f", 0.4, 0.5),
      compute: make("#0c1a24", 0.5, 0.4),
      sensor: make("#101317", 0.4, 0.45),
      lens: new THREE.MeshStandardMaterial({ color: "#04060a", metalness: 0.2, roughness: 0.08 }),
    };
  }, [highlight]);
}
type Mats = ReturnType<typeof useMaterials>;

/** Pick the chassis material, swapped for the glow when its category is active. */
function matFor(mats: Mats, cat: Category, base: THREE.Material): THREE.Material {
  return mats.highlight === cat ? mats.accent : base;
}

function partOf(design: Design, cat: Category): Part | undefined {
  const id = singleSelected(design, cat);
  return id ? getPart(id) : undefined;
}

function Robot({ design, highlight }: { design: Design; highlight: Category | null }) {
  const mats = useMaterials(highlight);
  const base = partOf(design, "base");
  const drive = partOf(design, "drive");
  const power = partOf(design, "power");
  const compute = partOf(design, "compute");
  const arm = partOf(design, "arm");
  const gripper = partOf(design, "gripper");
  const sensors = selectedIds(design, "sensor").map(getPart).filter(Boolean) as Part[];

  if (!base) {
    return (
      <group>
        {/* ghost platform invites the first part */}
        <mesh position={[0, 0.02, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[0.22, 0.24, 48]} />
          <meshBasicMaterial color={CYAN} transparent opacity={0.5} side={THREE.DoubleSide} />
        </mesh>
        <mesh position={[0, 0.02, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <circleGeometry args={[0.22, 48]} />
          <meshBasicMaterial color={CYAN} transparent opacity={0.06} side={THREE.DoubleSide} />
        </mesh>
      </group>
    );
  }

  const [L, W, H] = base.size;
  const wheelR = drive ? clamp(drive.size[2] / 2, 0.03, 0.09) : 0.05;
  const baseBottom = wheelR * 0.7;
  const baseY = baseBottom + H / 2;
  const baseTop = baseBottom + H;

  return (
    <group>
      {/* ── chassis ── */}
      <RoundedBox args={[L, H, W]} radius={0.008} smoothness={3} position={[0, baseY, 0]} castShadow receiveShadow material={matFor(mats, "base", mats.chassis)} />

      {/* cyan status strip along the front edge */}
      <mesh position={[L / 2 - 0.004, baseTop, 0]}>
        <boxGeometry args={[0.006, 0.004, W * 0.5]} />
        <primitive object={mats.accent} attach="material" />
      </mesh>

      {/* ── wheels ── */}
      {drive &&
        ([
          [L / 2 - wheelR * 0.6, W / 2 + wheelR * 0.25],
          [L / 2 - wheelR * 0.6, -(W / 2 + wheelR * 0.25)],
          [-(L / 2 - wheelR * 0.6), W / 2 + wheelR * 0.25],
          [-(L / 2 - wheelR * 0.6), -(W / 2 + wheelR * 0.25)],
        ] as [number, number][]).map(([x, z], i) => (
          <group key={i} position={[x, wheelR, z]} rotation={[Math.PI / 2, 0, 0]}>
            <mesh castShadow material={matFor(mats, "drive", mats.tire)}>
              <cylinderGeometry args={[wheelR, wheelR, wheelR * 0.8, 24]} />
            </mesh>
            <mesh position={[0, (z > 0 ? 1 : -1) * wheelR * 0.42, 0]} material={mats.hub}>
              <cylinderGeometry args={[wheelR * 0.42, wheelR * 0.42, wheelR * 0.06, 18]} />
            </mesh>
          </group>
        ))}

      {/* ── battery (rides low, under the deck) ── */}
      {power && (
        <RoundedBox
          args={[power.size[0], power.size[2], power.size[1]]}
          radius={0.004}
          smoothness={2}
          position={[-L * 0.05, baseBottom + power.size[2] / 2 + H * 0.05, 0]}
          castShadow
          material={matFor(mats, "power", mats.battery)}
        />
      )}

      {/* ── compute (on the deck) ── */}
      {compute && (
        <RoundedBox
          args={[compute.size[0], compute.size[2], compute.size[1]]}
          radius={0.003}
          smoothness={2}
          position={[-L * 0.18, baseTop + compute.size[2] / 2, W * 0.18]}
          castShadow
          material={matFor(mats, "compute", mats.compute)}
        />
      )}

      {/* ── arm ── */}
      {arm && (
        <ArmRig
          mats={mats}
          reach={arm.reach ?? 0.5}
          mountX={L * 0.12}
          mountY={baseTop}
          highlighted={highlight === "arm"}
          gripper={gripper}
          gripperHi={highlight === "gripper"}
        />
      )}

      {/* ── sensors ── */}
      {sensors.map((s, i) => (
        <SensorMesh key={s.id + i} part={s} index={i} L={L} W={W} baseTop={baseTop} mats={mats} hi={highlight === "sensor"} />
      ))}
    </group>
  );
}

function ArmRig({
  mats,
  reach,
  mountX,
  mountY,
  highlighted,
  gripper,
  gripperHi,
}: {
  mats: Mats;
  reach: number;
  mountX: number;
  mountY: number;
  highlighted: boolean;
  gripper?: Part;
  gripperHi: boolean;
}) {
  const armMat = highlighted ? mats.accent : mats.arm;
  const l1 = reach * 0.5;
  const l2 = reach * 0.42;
  const w = 0.04;
  return (
    <group position={[mountX, mountY, 0]}>
      {/* pedestal */}
      <RoundedBox args={[0.07, 0.05, 0.06]} radius={0.005} smoothness={2} position={[0, 0.025, 0]} castShadow material={armMat} />
      {/* shoulder servo */}
      <mesh position={[0, 0.055, 0]} material={mats.servo}>
        <cylinderGeometry args={[0.022, 0.022, 0.04, 20]} />
      </mesh>
      {/* lift → upper arm reaching up-forward */}
      <group position={[0, 0.07, 0]} rotation={[0, 0, -0.7]}>
        <RoundedBox args={[w, l1, w]} radius={0.006} smoothness={2} position={[0, l1 / 2, 0]} castShadow material={armMat} />
        {/* elbow */}
        <group position={[0, l1, 0]} rotation={[0, 0, 1.25]}>
          <mesh material={mats.servo}>
            <cylinderGeometry args={[0.019, 0.019, w * 1.1, 18]} />
          </mesh>
          <RoundedBox args={[w * 0.85, l2, w * 0.85]} radius={0.005} smoothness={2} position={[0, l2 / 2, 0]} castShadow material={armMat} />
          {/* wrist + gripper */}
          <group position={[0, l2, 0]} rotation={[0, 0, -0.5]}>
            <RoundedBox args={[0.05, 0.045, 0.045]} radius={0.005} smoothness={2} position={[0, 0.022, 0]} castShadow material={armMat} />
            {gripper && (
              <group position={[0, 0.05, 0]}>
                {[-1, 1].map((s) => (
                  <RoundedBox
                    key={s}
                    args={[0.012, 0.04, 0.012]}
                    radius={0.003}
                    smoothness={2}
                    position={[0, 0.02, s * 0.014]}
                    castShadow
                    material={gripperHi ? mats.accent : armMat}
                  />
                ))}
              </group>
            )}
          </group>
        </group>
      </group>
    </group>
  );
}

function SensorMesh({
  part,
  index,
  L,
  W,
  baseTop,
  mats,
  hi,
}: {
  part: Part;
  index: number;
  L: number;
  W: number;
  baseTop: number;
  mats: Mats;
  hi: boolean;
}) {
  const m = hi ? mats.accent : mats.sensor;
  const off = index * 0.012;
  const isLidar = part.id.includes("lidar");
  const isDepth = part.id.includes("astra") || part.id.includes("depth");
  const isImu = part.id.includes("imu");

  if (isLidar) {
    return (
      <group position={[-L * 0.18, baseTop, -W * 0.18 - off]}>
        <mesh position={[0, 0.03, 0]} material={mats.servo}>
          <cylinderGeometry args={[0.006, 0.006, 0.06, 10]} />
        </mesh>
        <mesh position={[0, 0.065, 0]} castShadow material={m}>
          <cylinderGeometry args={[Math.max(0.03, part.size[0] / 2), Math.max(0.03, part.size[0] / 2), part.size[2], 24]} />
        </mesh>
      </group>
    );
  }
  if (isDepth) {
    return (
      <group position={[L * 0.36, baseTop + 0.03, 0]}>
        <RoundedBox args={[part.size[1], part.size[2], part.size[0]]} radius={0.004} smoothness={2} castShadow material={m} />
        {[-0.05, 0, 0.05].map((z, i) => (
          <mesh key={i} position={[part.size[1] / 2, 0, z]} rotation={[0, 0, Math.PI / 2]} material={i === 1 ? mats.accent : mats.lens}>
            <cylinderGeometry args={[0.009, 0.009, 0.004, 16]} />
          </mesh>
        ))}
      </group>
    );
  }
  if (isImu) {
    return (
      <RoundedBox args={[part.size[0], part.size[2], part.size[1]]} radius={0.002} smoothness={1} position={[0, baseTop + part.size[2] / 2, -W * 0.05]} material={m} />
    );
  }
  // generic camera
  return (
    <group position={[L * 0.42, baseTop + 0.04 + off, W * (index % 2 ? 0.18 : -0.18)]}>
      <RoundedBox args={[part.size[0], part.size[1], part.size[2]]} radius={0.003} smoothness={2} castShadow material={m} />
      <mesh position={[part.size[0] / 2, 0, 0]} rotation={[0, 0, Math.PI / 2]} material={mats.lens}>
        <cylinderGeometry args={[0.006, 0.006, 0.004, 14]} />
      </mesh>
    </group>
  );
}

function clamp(v: number, lo: number, hi: number): number {
  return Math.max(lo, Math.min(hi, v));
}

export default function BuildCanvas({ design, highlight }: { design: Design; highlight: Category | null }) {
  return (
    <Canvas
      shadows
      dpr={[1, 2]}
      gl={{ antialias: true, alpha: true, toneMapping: THREE.ACESFilmicToneMapping, toneMappingExposure: 1.05 }}
      camera={{ position: [0.62, 0.46, 0.66], fov: 38, near: 0.05, far: 50 }}
      style={{ width: "100%", height: "100%" }}
    >
      <Suspense fallback={null}>
        <ambientLight intensity={0.4} />
        <hemisphereLight args={["#cfe9ff", "#05070d", 0.5]} />
        <directionalLight
          position={[1.4, 2.4, 1.2]}
          intensity={2.6}
          castShadow
          shadow-mapSize={[1024, 1024]}
          shadow-bias={-0.0002}
        >
          <orthographicCamera attach="shadow-camera" args={[-0.7, 0.7, 0.7, -0.7, 0.1, 6]} />
        </directionalLight>
        <spotLight position={[-1.6, 1.2, -1.2]} angle={0.6} penumbra={1} intensity={6} color={CYAN} distance={6} />

        <Environment resolution={256} frames={1}>
          <Lightformer intensity={1.6} position={[0, 2.4, 0]} scale={[4, 4, 1]} color="#ffffff" />
          <Lightformer intensity={1.2} position={[-2.4, 1, -1.5]} scale={[2.4, 2.4, 1]} color={CYAN} />
          <Lightformer intensity={1.0} position={[2.4, 1, -1.5]} scale={[2.4, 2.4, 1]} color="#7c3aed" />
        </Environment>

        <Robot design={design} highlight={highlight} />

        <ContactShadows position={[0, 0.001, 0]} opacity={0.45} scale={1.6} blur={2.4} far={0.7} resolution={1024} color="#000000" />
        <Grid
          position={[0, 0, 0]}
          args={[8, 8]}
          cellSize={0.1}
          cellThickness={0.5}
          cellColor="#10293d"
          sectionSize={0.5}
          sectionThickness={1}
          sectionColor="#0a6f8c"
          fadeDistance={3.2}
          fadeStrength={2.2}
          followCamera={false}
          infiniteGrid
        />

        <OrbitControls
          makeDefault
          enablePan={false}
          minDistance={0.45}
          maxDistance={2.2}
          minPolarAngle={0.2}
          maxPolarAngle={1.45}
          target={[0, 0.12, 0]}
          enableDamping
          dampingFactor={0.08}
        />
      </Suspense>
    </Canvas>
  );
}
