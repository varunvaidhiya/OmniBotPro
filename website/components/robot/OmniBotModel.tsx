"use client";

/*
 * OmniBotModel — a faithful, interactive Three.js rebuild of the OmniBot
 * mecanum mobile-manipulator described in
 * robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro.
 *
 * Coordinate mapping  (URDF is Z-up / X-forward, three.js is Y-up):
 *     three.x =  urdf.x   (forward)
 *     three.y =  urdf.z   (up)
 *     three.z = -urdf.y   (right)
 *
 * Interaction (driven from useFrame):
 *   • Arrow keys / WASD  → drive the base (mecanum: forward, strafe, turn)
 *   • Mouse pointer      → the SO-101 arm aims toward the cursor
 */

import { useFrame, useThree } from "@react-three/fiber";
import { useEffect, useMemo, useRef } from "react";
import * as THREE from "three";

/* ── URDF constants (metres) ─────────────────────────────────────── */
const PLATE_T = 0.006;
const STANDOFF_H = 0.16;
const GROUND_CLEAR = 0.07;
const CHASSIS_L = 0.265;
const CHASSIS_W = 0.25;
const BASE_Z = GROUND_CLEAR + PLATE_T / 2; // bottom-plate centre height
const TOP_DZ = PLATE_T + STANDOFF_H; // top-plate centre above base centre
const TOP_Z = BASE_Z + TOP_DZ;
const TOP_SURFACE_Z = TOP_Z + PLATE_T / 2;
const WHEEL_R = 0.04;
const WHEEL_W = 0.037;
const WHEEL_X = 0.165 / 2;
const WHEEL_Y = 0.215 / 2;

/* convert URDF (x,y,z) → three position tuple */
const P = (x: number, y: number, z: number): [number, number, number] => [x, z, -y];

/* ── shared materials ────────────────────────────────────────────── */
function useMaterials() {
  return useMemo(() => {
    return {
      chassis: new THREE.MeshStandardMaterial({
        color: "#0b0b0e",
        metalness: 0.45,
        roughness: 0.55,
      }),
      gold: new THREE.MeshStandardMaterial({
        color: "#c9a24b",
        metalness: 0.95,
        roughness: 0.28,
      }),
      armWhite: new THREE.MeshStandardMaterial({
        color: "#eef0f2",
        metalness: 0.1,
        roughness: 0.45,
      }),
      servo: new THREE.MeshStandardMaterial({
        color: "#16181d",
        metalness: 0.55,
        roughness: 0.4,
      }),
      hub: new THREE.MeshStandardMaterial({
        color: "#0a0a0c",
        metalness: 0.5,
        roughness: 0.5,
      }),
      roller: new THREE.MeshStandardMaterial({
        color: "#9aa0a6",
        metalness: 0.85,
        roughness: 0.35,
      }),
      lens: new THREE.MeshStandardMaterial({
        color: "#05070c",
        metalness: 0.2,
        roughness: 0.1,
      }),
      accent: new THREE.MeshStandardMaterial({
        color: "#00d4ff",
        emissive: new THREE.Color("#00d4ff"),
        emissiveIntensity: 1.6,
        metalness: 0.3,
        roughness: 0.4,
      }),
    };
  }, []);
}

type Mats = ReturnType<typeof useMaterials>;

/* ── one mecanum wheel (hub + diagonal barrel rollers) ───────────── */
function MecanumWheel({
  mats,
  chir,
  innerRef,
}: {
  mats: Mats;
  chir: 1 | -1;
  innerRef: React.MutableRefObject<THREE.Group | null>;
}) {
  const rollers = useMemo(() => {
    const N = 11;
    const ringR = WHEEL_R - 0.006;
    return Array.from({ length: N }, (_, i) => ({
      phi: (i / N) * Math.PI * 2,
      ringR,
      key: i,
    }));
  }, []);

  return (
    <group ref={innerRef}>
      {/* hub — axle is along three.z (the wheel axle) */}
      <mesh castShadow rotation={[Math.PI / 2, 0, 0]} material={mats.hub}>
        <cylinderGeometry args={[WHEEL_R * 0.82, WHEEL_R * 0.82, WHEEL_W, 28]} />
      </mesh>
      {/* outer rim hint */}
      <mesh rotation={[Math.PI / 2, 0, 0]} material={mats.hub}>
        <torusGeometry args={[WHEEL_R * 0.84, 0.004, 8, 32]} />
      </mesh>
      {/* barrel rollers */}
      {rollers.map(({ phi, ringR, key }) => (
        <group key={key} rotation={[0, 0, phi]}>
          <mesh
            position={[ringR, 0, 0]}
            rotation={[chir * (Math.PI / 4), 0, Math.PI / 2]}
            castShadow
            material={mats.roller}
          >
            <cylinderGeometry args={[0.0075, 0.0075, WHEEL_W * 0.92, 10]} />
          </mesh>
        </group>
      ))}
    </group>
  );
}

/* ── box helper expressed in URDF space ──────────────────────────── */
function Box({
  size,
  pos,
  mat,
}: {
  size: [number, number, number]; // URDF (sx, sy, sz)
  pos: [number, number, number]; // URDF (x, y, z)
  mat: THREE.Material;
}) {
  // three box extents: x=sx, y=sz, z=sy
  return (
    <mesh position={P(pos[0], pos[1], pos[2])} castShadow receiveShadow material={mat}>
      <boxGeometry args={[size[0], size[2], size[1]]} />
    </mesh>
  );
}

export default function OmniBotModel() {
  const mats = useMaterials();
  const { pointer } = useThree();

  /* group refs */
  const root = useRef<THREE.Group>(null);
  const wheelFL = useRef<THREE.Group>(null);
  const wheelFR = useRef<THREE.Group>(null);
  const wheelRL = useRef<THREE.Group>(null);
  const wheelRR = useRef<THREE.Group>(null);

  /* arm joint group refs */
  const panG = useRef<THREE.Group>(null);
  const liftG = useRef<THREE.Group>(null);
  const elbowG = useRef<THREE.Group>(null);
  const wristG = useRef<THREE.Group>(null);
  const jawL = useRef<THREE.Group>(null);
  const jawR = useRef<THREE.Group>(null);

  /* mutable driving state */
  const keys = useRef<Record<string, boolean>>({});
  const vel = useRef({ x: 0, y: 0, w: 0 }); // body-frame velocities
  const heading = useRef(0);
  const wheelSpin = useRef(0);

  /* arm target angles (smoothed) */
  const arm = useRef({ pan: 0, lift: -0.5, elbow: 1.0, wrist: 0.4, grip: 0 });

  useEffect(() => {
    const tracked = [
      "arrowup",
      "arrowdown",
      "arrowleft",
      "arrowright",
      "w",
      "a",
      "s",
      "d",
      "q",
      "e",
    ];
    const down = (e: KeyboardEvent) => {
      const k = e.key.toLowerCase();
      if (!tracked.includes(k)) return;
      // only capture driving keys while the hero is on screen, so arrow-key
      // scrolling still works once the user has scrolled past it
      const heroVisible = window.scrollY < window.innerHeight * 0.6;
      if (!heroVisible) return;
      keys.current[k] = true;
      if (k.startsWith("arrow")) e.preventDefault(); // stop page scroll
    };
    const up = (e: KeyboardEvent) => {
      keys.current[e.key.toLowerCase()] = false;
    };
    window.addEventListener("keydown", down, { passive: false });
    window.addEventListener("keyup", up);
    return () => {
      window.removeEventListener("keydown", down);
      window.removeEventListener("keyup", up);
    };
  }, []);

  useFrame((_, dtRaw) => {
    const dt = Math.min(dtRaw, 0.05);
    const k = keys.current;

    /* ── target body velocities from keys ── */
    const FWD = 0.55;
    const STRAFE = 0.5;
    const TURN = 1.6;
    let tx = 0,
      ty = 0,
      tw = 0;
    if (k["arrowup"] || k["w"]) tx += FWD;
    if (k["arrowdown"] || k["s"]) tx -= FWD;
    if (k["q"]) ty += STRAFE;
    if (k["e"]) ty -= STRAFE;
    if (k["arrowleft"] || k["a"]) tw += TURN;
    if (k["arrowright"] || k["d"]) tw -= TURN;

    // smooth (the Yahboom board ramp-limits — mimic it)
    const ramp = Math.min(1, 4 * dt);
    vel.current.x += (tx - vel.current.x) * ramp;
    vel.current.y += (ty - vel.current.y) * ramp;
    vel.current.w += (tw - vel.current.w) * ramp;

    /* ── integrate pose ── */
    heading.current += vel.current.w * dt;
    const h = heading.current;
    const dxWorld = (vel.current.x * Math.cos(h) - vel.current.y * Math.sin(h)) * dt;
    const dyWorld = (vel.current.x * Math.sin(h) + vel.current.y * Math.cos(h)) * dt;

    if (root.current) {
      root.current.position.x += dxWorld;
      root.current.position.z += -dyWorld;
      root.current.rotation.y = h;

      // keep the robot inside a tasteful arena
      const R = 1.05;
      const px = root.current.position.x;
      const pz = root.current.position.z;
      const d = Math.hypot(px, pz);
      if (d > R) {
        root.current.position.x = (px / d) * R;
        root.current.position.z = (pz / d) * R;
      }
    }

    /* ── wheel spin ── */
    wheelSpin.current += (vel.current.x / WHEEL_R) * dt;
    const baseSpin = wheelSpin.current;
    const turn = (vel.current.w * WHEEL_Y) / WHEEL_R;
    if (wheelFL.current) wheelFL.current.rotation.z = -(baseSpin - turn);
    if (wheelFR.current) wheelFR.current.rotation.z = -(baseSpin + turn);
    if (wheelRL.current) wheelRL.current.rotation.z = -(baseSpin - turn);
    if (wheelRR.current) wheelRR.current.rotation.z = -(baseSpin + turn);

    /* ── arm follows the pointer ── */
    const targetPan = THREE.MathUtils.clamp(-pointer.x * 1.4, -1.9, 1.9);
    const reach = THREE.MathUtils.clamp(pointer.y, -1, 1);
    const targetLift = THREE.MathUtils.clamp(-0.35 - reach * 0.85, -1.6, 0.7);
    const targetElbow = THREE.MathUtils.clamp(0.95 - reach * 0.7, -0.2, 1.6);
    const targetWrist = THREE.MathUtils.clamp(0.4 + reach * 0.5, -1.0, 1.4);
    const targetGrip = 0.25 + Math.sin(performance.now() / 700) * 0.2;

    const ease = Math.min(1, 6 * dt);
    arm.current.pan += (targetPan - arm.current.pan) * ease;
    arm.current.lift += (targetLift - arm.current.lift) * ease;
    arm.current.elbow += (targetElbow - arm.current.elbow) * ease;
    arm.current.wrist += (targetWrist - arm.current.wrist) * ease;
    arm.current.grip += (targetGrip - arm.current.grip) * ease;

    if (panG.current) panG.current.rotation.y = arm.current.pan;
    if (liftG.current) liftG.current.rotation.z = arm.current.lift;
    if (elbowG.current) elbowG.current.rotation.z = arm.current.elbow;
    if (wristG.current) wristG.current.rotation.z = arm.current.wrist;
    if (jawL.current) jawL.current.rotation.z = arm.current.grip;
    if (jawR.current) jawR.current.rotation.z = -arm.current.grip;
  });

  /* standoff positions from URDF */
  const standoffs: [number, number][] = [
    [0.095, 0.085],
    [0.065, 0.095],
    [0.095, -0.085],
    [0.065, -0.095],
    [-0.095, 0.085],
    [-0.065, 0.095],
    [-0.095, -0.085],
    [-0.065, -0.095],
  ];
  const standoffZ = BASE_Z + PLATE_T / 2 + STANDOFF_H / 2;

  return (
    <group ref={root}>
      {/* ── plates ── */}
      <Box size={[CHASSIS_L, CHASSIS_W, PLATE_T]} pos={[0, 0, BASE_Z]} mat={mats.chassis} />
      <Box size={[CHASSIS_L, CHASSIS_W, PLATE_T]} pos={[0, 0, TOP_Z]} mat={mats.chassis} />

      {/* ── 8 gold standoffs ── */}
      {standoffs.map(([x, y], i) => (
        <mesh key={i} position={P(x, y, standoffZ)} castShadow material={mats.gold}>
          <cylinderGeometry args={[0.007, 0.007, STANDOFF_H, 12]} />
        </mesh>
      ))}

      {/* ── electronics hint between the plates ── */}
      <Box size={[0.12, 0.16, 0.07]} pos={[-0.01, 0, BASE_Z + 0.06]} mat={mats.servo} />
      <Box size={[0.08, 0.045, 0.03]} pos={[0.02, 0, BASE_Z + 0.025]} mat={mats.lens} />
      {/* cyan status strip on the front edge of the top plate */}
      <Box size={[0.006, 0.1, 0.004]} pos={[CHASSIS_L / 2 - 0.004, 0, TOP_Z]} mat={mats.accent} />

      {/* ── front/rear side cameras (OV9732) ── */}
      <Box size={[0.03, 0.03, 0.025]} pos={[CHASSIS_L / 2 - 0.015, 0, BASE_Z + 0.0155]} mat={mats.servo} />
      <Box size={[0.03, 0.03, 0.025]} pos={[-(CHASSIS_L / 2 - 0.015), 0, BASE_Z + 0.0155]} mat={mats.servo} />

      {/* ── depth camera (Orbbec Astra) on the rear of the top plate ── */}
      <group position={P(-(CHASSIS_L / 2 - 0.03), 0, TOP_SURFACE_Z + 0.022)} rotation={[0, 0, 0.21]}>
        <mesh castShadow material={mats.chassis}>
          <boxGeometry args={[0.03, 0.04, 0.165]} />
        </mesh>
        <mesh position={[-0.016, 0, 0.042]} rotation={[0, 0, Math.PI / 2]} material={mats.lens}>
          <cylinderGeometry args={[0.011, 0.011, 0.006, 20]} />
        </mesh>
        <mesh position={[-0.016, 0, -0.042]} rotation={[0, 0, Math.PI / 2]} material={mats.lens}>
          <cylinderGeometry args={[0.011, 0.011, 0.006, 20]} />
        </mesh>
      </group>

      {/* ── four mecanum wheels (diagonal pairs share chirality) ── */}
      <group position={P(WHEEL_X, WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={1} innerRef={wheelFL} />
      </group>
      <group position={P(WHEEL_X, -WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={-1} innerRef={wheelFR} />
      </group>
      <group position={P(-WHEEL_X, WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={-1} innerRef={wheelRL} />
      </group>
      <group position={P(-WHEEL_X, -WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={1} innerRef={wheelRR} />
      </group>

      {/* ════════════════════════════════════════════════════════════
          SO-101 6-DOF arm — mounted on the top plate, aims at the mouse
          ════════════════════════════════════════════════════════════ */}
      <group position={P(0.055, 0, TOP_SURFACE_Z)}>
        {/* fixed pedestal */}
        <mesh position={[0, 0.028, 0]} castShadow material={mats.armWhite}>
          <boxGeometry args={[0.075, 0.055, 0.065]} />
        </mesh>

        {/* PAN (about vertical Y) */}
        <group ref={panG} position={[0, 0.058, 0]}>
          <mesh rotation={[Math.PI / 2, 0, 0]} castShadow material={mats.servo}>
            <cylinderGeometry args={[0.022, 0.022, 0.05, 20]} />
          </mesh>

          {/* LIFT */}
          <group ref={liftG} position={[0, 0.01, 0]}>
            <mesh position={[0.06, 0, 0]} castShadow material={mats.armWhite}>
              <boxGeometry args={[0.13, 0.032, 0.03]} />
            </mesh>
            <mesh position={[0.12, 0, 0]} rotation={[Math.PI / 2, 0, 0]} castShadow material={mats.servo}>
              <cylinderGeometry args={[0.019, 0.019, 0.042, 18]} />
            </mesh>

            {/* ELBOW */}
            <group ref={elbowG} position={[0.12, 0, 0]}>
              <mesh position={[0.058, 0, 0]} castShadow material={mats.armWhite}>
                <boxGeometry args={[0.125, 0.028, 0.026]} />
              </mesh>
              <mesh position={[0.115, 0, 0]} rotation={[Math.PI / 2, 0, 0]} castShadow material={mats.servo}>
                <cylinderGeometry args={[0.018, 0.018, 0.04, 18]} />
              </mesh>

              {/* WRIST */}
              <group ref={wristG} position={[0.115, 0, 0]}>
                <mesh position={[0.03, 0, 0]} castShadow material={mats.armWhite}>
                  <boxGeometry args={[0.05, 0.045, 0.04]} />
                </mesh>
                {/* wrist camera (cyan lens) */}
                <mesh position={[0.03, 0.03, 0]} castShadow material={mats.servo}>
                  <boxGeometry args={[0.022, 0.018, 0.02]} />
                </mesh>
                <mesh position={[0.042, 0.03, 0]} rotation={[0, 0, Math.PI / 2]} material={mats.accent}>
                  <cylinderGeometry args={[0.005, 0.005, 0.004, 16]} />
                </mesh>
                {/* jaws */}
                <group ref={jawL} position={[0.055, 0, 0.012]}>
                  <mesh position={[0.022, 0, 0]} castShadow material={mats.armWhite}>
                    <boxGeometry args={[0.05, 0.012, 0.01]} />
                  </mesh>
                </group>
                <group ref={jawR} position={[0.055, 0, -0.012]}>
                  <mesh position={[0.022, 0, 0]} castShadow material={mats.armWhite}>
                    <boxGeometry args={[0.05, 0.012, 0.01]} />
                  </mesh>
                </group>
              </group>
            </group>
          </group>
        </group>
      </group>
    </group>
  );
}
