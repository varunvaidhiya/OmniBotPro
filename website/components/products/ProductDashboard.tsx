/*
 * ProductDashboard — a bespoke, on-brand SVG "screenshot" of each product's
 * interface, embedded on its Learn-more page. Pure SVG (vector, crisp at any
 * size, version-controlled, no binary assets) which suits this statically
 * exported site with no /public directory.
 *
 * Add a product => add a <XDash/> and one row in DASHBOARDS.
 */

import {
  DashboardFrame,
  Panel,
  Ring,
  Bar,
  StatusPill,
  Dot,
  sparkPath,
  accentHex,
  C,
  MONO,
  DISPLAY,
  BODY,
  type Accent,
} from "./dashboardKit";

type DashProps = { accent: Accent };

// ── OhhO Build ────────────────────────────────────────────────────────────────
function BuildDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const parts = ["Mecanum base", "SO-101 arm", "Depth camera", "2-D LiDAR", "Li-ion battery", "Jetson Orin"];
  const req = [
    { l: "Payload", v: 0.78, t: "5.0 kg" },
    { l: "Reach", v: 0.62, t: "0.62 m" },
    { l: "Top speed", v: 0.55, t: "1.2 m/s" },
    { l: "Runtime", v: 0.7, t: "6.4 h" },
  ];
  return (
    <DashboardFrame title="ohho-build · warehouse-amr" accent={accent} tools={["3-D", "BOM"]}>
      {/* parts palette */}
      <Panel x={14} y={12} w={120} h={250} label="Parts" />
      {parts.map((p, i) => {
        const sel = i === 1;
        return (
          <g key={p}>
            <rect x={22} y={34 + i * 35} width={104} height={26} rx={6} fill={sel ? "rgba(0,212,255,0.10)" : "rgba(255,255,255,0.03)"} stroke={sel ? a : C.border} />
            <circle cx={33} cy={47 + i * 35} r={1.3} fill={C.faint} />
            <circle cx={33} cy={47 + i * 35 - 4} r={1.3} fill={C.faint} />
            <circle cx={33} cy={47 + i * 35 + 4} r={1.3} fill={C.faint} />
            <circle cx={38} cy={47 + i * 35} r={1.3} fill={C.faint} />
            <circle cx={38} cy={47 + i * 35 - 4} r={1.3} fill={C.faint} />
            <circle cx={38} cy={47 + i * 35 + 4} r={1.3} fill={C.faint} />
            <text x={48} y={51 + i * 35} fontFamily={BODY} fontSize={9.5} fill={sel ? C.text : C.muted}>
              {p}
            </text>
          </g>
        );
      })}

      {/* 3-D canvas */}
      <rect x={144} y={12} width={246} height={250} rx={9} fill="#0B1120" stroke={C.border} />
      {/* perspective floor */}
      {[0, 1, 2, 3].map((i) => (
        <line key={`h${i}`} x1={158 + i * 14} y1={150 + i * 24} x2={376 - i * 14} y2={150 + i * 24} stroke={C.grid} />
      ))}
      {[0, 1, 2, 3, 4].map((i) => (
        <line key={`v${i}`} x1={172 + i * 46} y1={150} x2={196 + i * 30} y2={222} stroke={C.grid} />
      ))}
      {/* robot base */}
      <rect x={232} y={150} width={70} height={22} rx={5} fill={C.panelHi} stroke={a} />
      <rect x={236} y={170} width={12} height={8} rx={2} fill={C.surfHi} stroke={C.borderHi} />
      <rect x={286} y={170} width={12} height={8} rx={2} fill={C.surfHi} stroke={C.borderHi} />
      <rect x={250} y={172} width={12} height={7} rx={2} fill={C.surfHi} stroke={C.borderHi} />
      <rect x={272} y={172} width={12} height={7} rx={2} fill={C.surfHi} stroke={C.borderHi} />
      {/* arm */}
      <polyline points="267,150 280,116 304,100 322,108" fill="none" stroke={a} strokeWidth={3} strokeLinecap="round" strokeLinejoin="round" />
      <circle cx={267} cy={150} r={4} fill={C.surf} stroke={a} strokeWidth={2} />
      <circle cx={280} cy={116} r={3.4} fill={C.surf} stroke={a} strokeWidth={2} />
      <circle cx={304} cy={100} r={3.4} fill={C.surf} stroke={a} strokeWidth={2} />
      <path d="M322 102 l7 -4 M322 114 l7 4" stroke={a} strokeWidth={2.4} strokeLinecap="round" />
      {/* dragged ghost part */}
      <rect x={196} y={60} width={70} height={24} rx={6} fill="rgba(0,212,255,0.08)" stroke={a} strokeDasharray="4 3" />
      <text x={231} y={75} fontFamily={BODY} fontSize={9} fill={a} textAnchor="middle">
        Gripper · drop
      </text>
      <text x={158} y={250} fontFamily={MONO} fontSize={8} fill={C.faint}>
        drag · scroll to zoom
      </text>

      {/* requirements */}
      <Panel x={400} y={12} w={146} h={132} label="Requirements" accent={a} />
      {req.map((r, i) => (
        <g key={r.l}>
          <text x={412} y={42 + i * 26} fontFamily={BODY} fontSize={9} fill={C.muted}>
            {r.l}
          </text>
          <text x={534} y={42 + i * 26} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">
            {r.t}
          </text>
          <Bar x={412} y={47 + i * 26} w={122} frac={r.v} color={a} h={4} />
        </g>
      ))}

      {/* AI recommendation */}
      <rect x={400} y={152} width={146} height={110} rx={9} fill="rgba(124,58,237,0.10)" stroke={C.violetLite} strokeOpacity={0.5} />
      <path d="M414 170 l-7 9 h6 l-1 8 7 -10 h-6 z" fill={C.violetLite} />
      <text x={426} y={177} fontFamily={MONO} fontSize={8.5} fill={C.violetLite}>
        AI RECOMMENDATION
      </text>
      <Dot cx={414} cy={196} r={2.4} color={C.green} />
      <text x={422} y={199} fontFamily={BODY} fontSize={9} fill={C.text}>
        Design is valid
      </text>
      <text x={412} y={218} fontFamily={BODY} fontSize={8.7} fill={C.muted}>
        Swap to an 11.1 V pack
      </text>
      <text x={412} y={230} fontFamily={BODY} fontSize={8.7} fill={C.muted}>
        for +18% runtime, −90 g.
      </text>
      <rect x={412} y={240} width={84} height={15} rx={7} fill={C.violetLite} />
      <text x={454} y={250} fontFamily={BODY} fontSize={8.5} fontWeight={600} fill={C.bg} textAnchor="middle">
        Apply &amp; re-check
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Frame ────────────────────────────────────────────────────────────────
function FrameDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const tree = [
    { t: "robot_ws/", d: 0, dir: true },
    { t: "src/", d: 1, dir: true },
    { t: "omnibot_driver", d: 2 },
    { t: "omnibot_navigation", d: 2 },
    { t: "omnibot_bringup", d: 2 },
    { t: "docker/", d: 0, dir: true },
    { t: "Dockerfile", d: 1 },
    { t: ".github/workflows", d: 0, dir: true },
  ];
  const nodes = [
    { x: 250, y: 40, l: "driver" },
    { x: 340, y: 40, l: "ekf" },
    { x: 430, y: 40, l: "nav2" },
    { x: 250, y: 96, l: "sim" },
    { x: 340, y: 96, l: "mux" },
    { x: 430, y: 96, l: "bringup" },
  ];
  const log = [
    { t: "colcon build --symlink-install", c: C.muted },
    { t: "Starting >>> omnibot_driver", c: C.faint },
    { t: "Finished <<< omnibot_navigation", c: C.faint },
    { t: "Summary: 12 packages finished", c: C.green },
    { t: "build complete · 0 errors", c: C.green },
  ];
  return (
    <DashboardFrame title="ohho-frame · robot_ws" accent={accent} tools={["ROS 2", "Docker", "CI"]}>
      {/* file tree */}
      <Panel x={14} y={12} w={128} h={250} label="Workspace" />
      {tree.map((n, i) => (
        <g key={n.t + i}>
          <text x={24 + n.d * 12} y={44 + i * 26} fontFamily={MONO} fontSize={9} fill={n.dir ? a : C.muted}>
            {n.dir ? "▸ " : ""}
            {n.t}
          </text>
        </g>
      ))}

      {/* node graph */}
      <Panel x={150} y={12} w={396} h={128} label="Node graph" />
      <line x1={282} y1={77} x2={308} y2={77} stroke={C.border} />
      <line x1={372} y1={77} x2={398} y2={77} stroke={C.border} />
      <line x1={282} y1={133} x2={308} y2={133} stroke={C.border} />
      <line x1={372} y1={133} x2={398} y2={133} stroke={C.border} />
      <line x1={265} y1={92} x2={265} y2={113} stroke={C.border} />
      <line x1={445} y1={92} x2={445} y2={113} stroke={C.border} />
      {nodes.map((n) => (
        <g key={n.l}>
          <rect x={n.x} y={n.y + 18} width={64} height={30} rx={7} fill={C.panelHi} stroke={C.borderHi} />
          <circle cx={n.x + 12} cy={n.y + 33} r={2.6} fill={a} />
          <text x={n.x + 20} y={n.y + 36} fontFamily={MONO} fontSize={8.5} fill={C.text}>
            {n.l}
          </text>
        </g>
      ))}

      {/* terminal */}
      <rect x={150} y={150} width={396} height={112} rx={9} fill="#0A0F1C" stroke={C.border} />
      <text x={162} y={170} fontFamily={MONO} fontSize={8.5} fill={C.faint}>
        ~/robot_ws · bash
      </text>
      {log.map((l, i) => (
        <text key={i} x={162} y={188 + i * 14} fontFamily={MONO} fontSize={8.5} fill={l.c}>
          {i >= 3 ? "" : "$ "}
          {l.t}
        </text>
      ))}
      <rect x={162} y={250} width={6} height={9} fill={a} />
    </DashboardFrame>
  );
}

// ── OhhO Serve ────────────────────────────────────────────────────────────────
function ServeDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const lat = [0.4, 0.45, 0.38, 0.5, 0.42, 0.6, 0.48, 0.52, 0.44, 0.4, 0.58, 0.46];
  const eps = [
    { m: "POST", p: "/predict", c: C.green },
    { m: "GET", p: "/health", c: C.blue },
    { m: "POST", p: "/load_model", c: C.amber },
  ];
  return (
    <DashboardFrame title="ohho-serve · inference" accent={accent} tools={["smolvla", "GPU 0"]}>
      {/* endpoints */}
      <Panel x={14} y={12} w={150} h={250} label="Endpoints" />
      {eps.map((e, i) => (
        <g key={e.p}>
          <rect x={22} y={34 + i * 34} width={134} height={26} rx={6} fill="rgba(255,255,255,0.03)" stroke={C.border} />
          <rect x={28} y={40 + i * 34} width={32} height={14} rx={4} fill={e.c} fillOpacity={0.18} stroke={e.c} strokeOpacity={0.5} />
          <text x={44} y={50 + i * 34} fontFamily={MONO} fontSize={7} fill={e.c} textAnchor="middle">
            {e.m}
          </text>
          <text x={66} y={51 + i * 34} fontFamily={MONO} fontSize={8.5} fill={C.text}>
            {e.p}
          </text>
        </g>
      ))}
      {/* request/response */}
      <rect x={22} y={146} width={134} height={108} rx={7} fill="#0A0F1C" stroke={C.border} />
      <text x={30} y={162} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        POST /predict
      </text>
      <text x={30} y={178} fontFamily={MONO} fontSize={7.5} fill={C.muted}>
        {"{ image, prompt }"}
      </text>
      <text x={30} y={198} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        200 OK · 41 ms
      </text>
      <text x={30} y={214} fontFamily={MONO} fontSize={7.5} fill={a}>
        {"action: ["}
      </text>
      <text x={36} y={226} fontFamily={MONO} fontSize={7.5} fill={C.muted}>
        0.12, 0.0, -0.4,
      </text>
      <text x={36} y={238} fontFamily={MONO} fontSize={7.5} fill={C.muted}>
        0.0, 0.0, 0.08 ]
      </text>

      {/* latency chart */}
      <Panel x={176} y={12} w={224} h={150} label="Latency · p50" />
      <text x={388} y={30} fontFamily={DISPLAY} fontSize={15} fontWeight={700} fill={C.text} textAnchor="end">
        41 ms
      </text>
      {[0, 1, 2].map((i) => (
        <line key={i} x1={188} y1={56 + i * 30} x2={388} y2={56 + i * 30} stroke={C.grid} />
      ))}
      <path d={sparkPath(lat, 188, 50, 200, 86, true)} fill={a} fillOpacity={0.12} stroke="none" />
      <path d={sparkPath(lat, 188, 50, 200, 86)} fill="none" stroke={a} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
      <text x={188} y={152} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        −60s
      </text>
      <text x={388} y={152} fontFamily={MONO} fontSize={7.5} fill={C.faint} textAnchor="end">
        now
      </text>

      {/* throughput */}
      <Panel x={176} y={172} w={224} h={90} label="Throughput" />
      <text x={188} y={216} fontFamily={DISPLAY} fontSize={20} fontWeight={700} fill={a}>
        23.6
      </text>
      <text x={250} y={216} fontFamily={MONO} fontSize={8.5} fill={C.muted}>
        req/s
      </text>
      <text x={188} y={240} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        4,812 today · 500 / day limit
      </text>
      <Bar x={188} y={246} w={200} frac={0.62} color={C.green} h={4} />

      {/* GPU */}
      <Panel x={412} y={12} w={134} h={250} label="GPU" />
      <Ring cx={479} cy={86} r={36} frac={0.74} color={a} width={9} label="74%" sub="UTIL" />
      <text x={428} y={150} fontFamily={BODY} fontSize={9} fill={C.muted}>
        VRAM
      </text>
      <text x={532} y={150} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">
        11.8 / 16 GB
      </text>
      <Bar x={428} y={156} w={104} frac={0.74} color={a} h={5} />
      <text x={428} y={184} fontFamily={BODY} fontSize={9} fill={C.muted}>
        Temp
      </text>
      <text x={532} y={184} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">
        61°C
      </text>
      <Bar x={428} y={190} w={104} frac={0.55} color={C.green} h={5} />
      <StatusPill x={428} y={214} label="4-bit · loaded" color={C.green} />
      <StatusPill x={428} y={236} label="healthy" color={a} />
    </DashboardFrame>
  );
}

// ── OhhO View ─────────────────────────────────────────────────────────────────
function ViewDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const cams = ["FRONT", "REAR", "LEFT", "RIGHT"];
  return (
    <DashboardFrame title="ohho-view · surround-bev" accent={accent} tools={["4 cams", "CPU"]}>
      {/* raw cameras */}
      <Panel x={14} y={12} w={150} h={250} label="Cameras" />
      {cams.map((c, i) => {
        const cx = 24 + (i % 2) * 70;
        const cy = 36 + Math.floor(i / 2) * 108;
        return (
          <g key={c}>
            <rect x={cx} y={cy} width={62} height={92} rx={6} fill="#0B1322" stroke={C.border} />
            {/* faux scene */}
            <rect x={cx} y={cy + 56} width={62} height={36} rx={6} fill="rgba(255,255,255,0.04)" />
            <circle cx={cx + 20} cy={cy + 44} r={7} fill="rgba(255,255,255,0.07)" />
            <rect x={cx + 38} y={cy + 30} width={14} height={20} rx={2} fill="rgba(255,255,255,0.07)" />
            <circle cx={cx + 6} cy={cy + 8} r={2} fill={C.green} />
            <text x={cx + 14} y={cy + 11} fontFamily={MONO} fontSize={6.5} fill={C.muted}>
              {c}
            </text>
          </g>
        );
      })}

      {/* fused BEV */}
      <Panel x={176} y={12} w={370} h={250} label="Fused bird's-eye-view" accent={a} />
      <text x={534} y={29} fontFamily={MONO} fontSize={8} fill={C.green} textAnchor="end">
        ● calibrated
      </text>
      <circle cx={361} cy={148} r={104} fill="#0A1424" stroke={C.border} />
      <circle cx={361} cy={148} r={104} fill={a} fillOpacity={0.04} />
      {/* range rings */}
      <circle cx={361} cy={148} r={70} fill="none" stroke={C.grid} />
      <circle cx={361} cy={148} r={36} fill="none" stroke={C.grid} />
      <line x1={361} y1={44} x2={361} y2={252} stroke={C.grid} />
      <line x1={257} y1={148} x2={465} y2={148} stroke={C.grid} />
      {/* robot footprint */}
      <rect x={345} y={132} width={32} height={32} rx={5} fill={C.panelHi} stroke={a} strokeWidth={1.6} />
      <path d="M361 138 v8" stroke={a} strokeWidth={2} strokeLinecap="round" />
      {/* obstacles */}
      <circle cx={410} cy={110} r={9} fill={C.amber} fillOpacity={0.5} stroke={C.amber} />
      <rect x={300} y={185} width={20} height={12} rx={3} fill={C.red} fillOpacity={0.4} stroke={C.red} />
      <circle cx={320} cy={96} r={6} fill="rgba(255,255,255,0.18)" />
      {/* stitch seams */}
      <line x1={361} y1={148} x2={300} y2={70} stroke={a} strokeOpacity={0.25} strokeDasharray="3 3" />
      <line x1={361} y1={148} x2={430} y2={70} stroke={a} strokeOpacity={0.25} strokeDasharray="3 3" />
      <line x1={361} y1={148} x2={300} y2={226} stroke={a} strokeOpacity={0.25} strokeDasharray="3 3" />
      <line x1={361} y1={148} x2={430} y2={226} stroke={a} strokeOpacity={0.25} strokeDasharray="3 3" />
    </DashboardFrame>
  );
}

// ── OhhO Data ─────────────────────────────────────────────────────────────────
function DataDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const rows = [
    { id: "ep_0148", f: "412", s: "kept", c: C.green },
    { id: "ep_0149", f: "388", s: "kept", c: C.green },
    { id: "ep_0150", f: "97", s: "discard", c: C.red },
    { id: "ep_0151", f: "455", s: "review", c: C.amber },
  ];
  const keys = [0.18, 0.34, 0.5, 0.66, 0.82];
  return (
    <DashboardFrame title="ohho-data · mobile_manipulation" accent={accent} tools={["LeRobot", "1,043 eps"]}>
      {/* episode table */}
      <Panel x={14} y={12} w={244} h={250} label="Episodes" />
      <text x={26} y={44} fontFamily={MONO} fontSize={8} fill={C.faint}>
        ID
      </text>
      <text x={150} y={44} fontFamily={MONO} fontSize={8} fill={C.faint}>
        FRAMES
      </text>
      <text x={210} y={44} fontFamily={MONO} fontSize={8} fill={C.faint}>
        STATUS
      </text>
      <line x1={24} y1={52} x2={248} y2={52} stroke={C.border} />
      {rows.map((r, i) => (
        <g key={r.id}>
          <rect x={22} y={60 + i * 34} width={228} height={28} rx={5} fill={i === 1 ? "rgba(0,212,255,0.06)" : "transparent"} />
          <text x={28} y={78 + i * 34} fontFamily={MONO} fontSize={9} fill={C.text}>
            {r.id}
          </text>
          <text x={156} y={78 + i * 34} fontFamily={MONO} fontSize={9} fill={C.muted}>
            {r.f}
          </text>
          <circle cx={216} cy={74 + i * 34} r={2.6} fill={r.c} />
          <text x={224} y={78 + i * 34} fontFamily={BODY} fontSize={8.5} fill={r.c}>
            {r.s}
          </text>
        </g>
      ))}

      {/* viewer */}
      <Panel x={270} y={12} w={276} h={150} label="Episode viewer · ep_0149" accent={a} />
      <rect x={280} y={40} width={120} height={92} rx={6} fill="#0B1322" stroke={C.border} />
      <circle cx={326} cy={78} r={14} fill="rgba(255,255,255,0.08)" />
      <rect x={356} y={92} width={28} height={28} rx={3} fill={C.amber} fillOpacity={0.4} stroke={C.amber} />
      <text x={286} y={52} fontFamily={MONO} fontSize={6.5} fill={C.green}>
        ● wrist cam
      </text>
      {/* state plot */}
      <rect x={410} y={40} width={126} height={92} rx={6} fill="#0A0F1C" stroke={C.border} />
      <text x={418} y={52} fontFamily={MONO} fontSize={6.5} fill={C.faint}>
        9-DOF state
      </text>
      <path d={sparkPath([0.5, 0.6, 0.55, 0.7, 0.62, 0.5, 0.45, 0.58], 418, 60, 110, 30)} fill="none" stroke={a} strokeWidth={1.6} />
      <path d={sparkPath([0.3, 0.35, 0.5, 0.45, 0.6, 0.65, 0.55, 0.5], 418, 96, 110, 30)} fill="none" stroke={C.violetLite} strokeWidth={1.6} />

      {/* timeline */}
      <Panel x={270} y={172} w={276} h={90} label="Timeline" />
      <line x1={282} y1={216} x2={534} y2={216} stroke={C.track} strokeWidth={4} strokeLinecap="round" />
      <line x1={282} y1={216} x2={420} y2={216} stroke={a} strokeWidth={4} strokeLinecap="round" />
      {keys.map((k, i) => (
        <circle key={i} cx={282 + k * 252} cy={216} r={3} fill={C.violetLite} />
      ))}
      <circle cx={420} cy={216} r={5.5} fill={C.text} stroke={a} strokeWidth={2} />
      <text x={282} y={240} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        00:00
      </text>
      <text x={534} y={240} fontFamily={MONO} fontSize={7.5} fill={C.faint} textAnchor="end">
        00:13
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Pilot ────────────────────────────────────────────────────────────────
function PilotDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const joints = ["pan", "lift", "elbow", "wrist", "roll", "grip"];
  const jv = [0.55, 0.4, 0.62, 0.5, 0.7, 0.3];
  return (
    <DashboardFrame title="ohho-pilot · teleop" accent={accent} tools={["VR", "28 ms"]}>
      {/* main robot view */}
      <rect x={14} y={12} width={340} height={250} rx={9} fill="#0A1322" stroke={C.border} />
      {/* horizon / scene */}
      <rect x={14} y={150} width={340} height={112} rx={9} fill="rgba(255,255,255,0.03)" />
      <line x1={14} y1={150} x2={354} y2={150} stroke={C.grid} />
      <ellipse cx={184} cy={205} rx={70} ry={20} fill="rgba(255,255,255,0.04)" />
      {/* target object + bbox */}
      <rect x={196} y={150} width={44} height={48} rx={4} fill={C.amber} fillOpacity={0.25} stroke={C.amber} strokeWidth={1.4} />
      <rect x={192} y={146} width={52} height={56} rx={3} fill="none" stroke={a} strokeWidth={1.6} strokeDasharray="5 3" />
      <text x={192} y={140} fontFamily={MONO} fontSize={8} fill={a}>
        cup · 0.94
      </text>
      {/* crosshair */}
      <circle cx={184} cy={130} r={16} fill="none" stroke={C.text} strokeOpacity={0.4} />
      <line x1={184} y1={108} x2={184} y2={120} stroke={C.text} strokeOpacity={0.4} />
      <line x1={184} y1={140} x2={184} y2={152} stroke={C.text} strokeOpacity={0.4} />
      <line x1={162} y1={130} x2={174} y2={130} stroke={C.text} strokeOpacity={0.4} />
      <line x1={194} y1={130} x2={206} y2={130} stroke={C.text} strokeOpacity={0.4} />
      <StatusPill x={24} y={24} label="● REC · front cam" color={C.red} />
      {/* hand tracking */}
      <text x={344} y={36} fontFamily={MONO} fontSize={7.5} fill={a} textAnchor="end">
        ✋ hand-tracking
      </text>
      <path d="M300 230 q6 -22 14 -2 q4 -18 10 -2 q4 -14 9 0 l1 16 q-2 12 -14 12 q-16 0 -20 -16 z" fill="rgba(0,212,255,0.10)" stroke={a} strokeWidth={1.4} strokeLinejoin="round" />

      {/* arm joints */}
      <Panel x={364} y={12} w={182} h={150} label="Arm · joint targets" accent={a} />
      {joints.map((j, i) => (
        <g key={j}>
          <text x={374} y={44 + i * 18} fontFamily={MONO} fontSize={8.5} fill={C.muted}>
            {j}
          </text>
          <Bar x={410} y={38 + i * 18} w={104} frac={jv[i]} color={a} h={4} />
          <text x={522} y={44 + i * 18} fontFamily={MONO} fontSize={7.5} fill={C.faint} textAnchor="end">
            {Math.round((jv[i] - 0.5) * 180)}°
          </text>
        </g>
      ))}

      {/* drive + status */}
      <Panel x={364} y={172} w={182} h={90} label="Drive" />
      <circle cx={398} cy={222} r={26} fill="rgba(255,255,255,0.03)" stroke={C.border} />
      <circle cx={406} cy={214} r={8} fill={a} />
      <line x1={398} y1={222} x2={406} y2={214} stroke={a} strokeWidth={2} />
      <text x={440} y={206} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        latency
      </text>
      <text x={534} y={206} fontFamily={MONO} fontSize={8.5} fill={C.green} textAnchor="end">
        28 ms
      </text>
      <rect x={440} y={230} width={94} height={22} rx={6} fill="rgba(248,113,113,0.14)" stroke={C.red} />
      <text x={487} y={244} fontFamily={DISPLAY} fontSize={9} fontWeight={700} fill={C.red} textAnchor="middle">
        E-STOP
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Fleet ────────────────────────────────────────────────────────────────
function FleetDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const pins = [
    { x: 70, y: 70, c: C.green },
    { x: 150, y: 110, c: C.green },
    { x: 110, y: 170, c: C.amber },
    { x: 220, y: 80, c: C.green },
    { x: 250, y: 165, c: C.red },
    { x: 180, y: 200, c: C.green },
  ];
  const rollout = [
    { l: "v2.4.0 · canary", v: 0.18, c: a },
    { l: "v2.3.1 · stable", v: 0.82, c: C.green },
  ];
  const alerts = [
    { t: "amr-17 · offline 4m", c: C.red },
    { t: "amr-05 · VLA latency ↑", c: C.amber },
    { t: "fleet · OTA 18% rolled", c: a },
  ];
  return (
    <DashboardFrame title="ohho-fleet · mission-control" accent={accent} tools={["48 online", "live"]}>
      {/* map */}
      <Panel x={14} y={12} w={300} h={250} label="Fleet map" />
      {[0, 1, 2, 3, 4].map((i) => (
        <line key={`mh${i}`} x1={20} y1={40 + i * 44} x2={308} y2={40 + i * 44} stroke={C.grid} />
      ))}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <line key={`mv${i}`} x1={30 + i * 48} y1={30} x2={30 + i * 48} y2={254} stroke={C.grid} />
      ))}
      {pins.map((p, i) => (
        <g key={i}>
          <circle cx={p.x + 14} cy={p.y + 30} r={9} fill={p.c} fillOpacity={0.18} />
          <circle cx={p.x + 14} cy={p.y + 30} r={4} fill={p.c} />
        </g>
      ))}

      {/* health donut */}
      <Panel x={322} y={12} w={224} h={104} label="Fleet health" accent={a} />
      <Ring cx={368} cy={70} r={30} frac={0.92} color={C.green} width={8} label="92%" sub="HEALTHY" />
      <g fontFamily={MONO} fontSize={8.5}>
        <Dot cx={414} cy={48} r={3} color={C.green} />
        <text x={422} y={51} fill={C.muted}>44 online</text>
        <Dot cx={414} cy={66} r={3} color={C.amber} />
        <text x={422} y={69} fill={C.muted}>3 degraded</text>
        <Dot cx={414} cy={84} r={3} color={C.red} />
        <text x={422} y={87} fill={C.muted}>1 offline</text>
      </g>

      {/* OTA rollout */}
      <Panel x={322} y={124} w={224} h={70} label="OTA rollout" />
      {rollout.map((r, i) => (
        <g key={r.l}>
          <text x={332} y={150 + i * 22} fontFamily={MONO} fontSize={8} fill={C.muted}>
            {r.l}
          </text>
          <text x={534} y={150 + i * 22} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="end">
            {Math.round(r.v * 48)}
          </text>
          <Bar x={332} y={154 + i * 22} w={202} frac={r.v} color={r.c} h={4} />
        </g>
      ))}

      {/* alerts */}
      <Panel x={322} y={202} w={224} h={60} label="Alerts" />
      {alerts.map((al, i) => (
        <g key={al.t}>
          <Dot cx={336} cy={226 + i * 14} r={2.4} color={al.c} />
          <text x={344} y={229 + i * 14} fontFamily={BODY} fontSize={8.3} fill={C.muted}>
            {al.t}
          </text>
        </g>
      ))}
    </DashboardFrame>
  );
}

// ── OhhO Comply ───────────────────────────────────────────────────────────────
function ComplyDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const stds = [
    { l: "EU Machinery Reg · CE", v: 1, s: "done", c: C.green },
    { l: "ISO 10218-1/2", v: 0.85, s: "12/14", c: C.green },
    { l: "ISO 13849 · PL d", v: 0.6, s: "review", c: C.amber },
    { l: "UL / IEC 60204", v: 0.4, s: "open", c: C.amber },
  ];
  const docs = [
    { l: "Technical file", c: C.green },
    { l: "Risk assessment", c: C.green },
    { l: "Declaration of Conformity", c: C.amber },
  ];
  return (
    <DashboardFrame title="ohho-comply · warehouse-amr" accent={accent} tools={["EU", "audit"]}>
      {/* standards checklist */}
      <Panel x={14} y={12} w={304} h={250} label="Applicable standards" accent={a} />
      {stds.map((st, i) => (
        <g key={st.l}>
          <rect x={24} y={36 + i * 50} width={284} height={40} rx={6} fill="rgba(255,255,255,0.02)" stroke={C.border} />
          {st.v >= 1 ? (
            <g>
              <circle cx={42} cy={56 + i * 50} r={8} fill={C.green} fillOpacity={0.2} stroke={C.green} />
              <path d={`M38 ${56 + i * 50} l3 3 l5 -6`} fill="none" stroke={C.green} strokeWidth={1.6} strokeLinecap="round" />
            </g>
          ) : (
            <circle cx={42} cy={56 + i * 50} r={8} fill="none" stroke={st.c} strokeWidth={1.6} strokeDasharray="2 2" />
          )}
          <text x={58} y={52 + i * 50} fontFamily={BODY} fontSize={9.5} fill={C.text}>
            {st.l}
          </text>
          <text x={300} y={52 + i * 50} fontFamily={MONO} fontSize={8} fill={st.c} textAnchor="end">
            {st.s}
          </text>
          <Bar x={58} y={62 + i * 50} w={242} frac={st.v} color={st.c} h={4} />
        </g>
      ))}

      {/* certification progress */}
      <Panel x={326} y={12} w={220} h={120} label="Certification" />
      <Ring cx={372} cy={76} r={32} frac={0.71} color={a} width={9} label="71%" sub="READY" />
      <text x={418} y={56} fontFamily={BODY} fontSize={9} fill={C.muted}>
        Requirements
      </text>
      <text x={418} y={70} fontFamily={DISPLAY} fontSize={13} fontWeight={700} fill={C.text}>
        38 / 54
      </text>
      <text x={418} y={92} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        Notified body
      </text>
      <StatusPill x={418} y={98} label="booked" color={C.green} />

      {/* documents */}
      <Panel x={326} y={140} w={220} h={68} label="Documents" />
      {docs.map((d, i) => (
        <g key={d.l}>
          <Dot cx={340} cy={166 + i * 14} r={2.6} color={d.c} />
          <text x={348} y={169 + i * 14} fontFamily={BODY} fontSize={8.3} fill={C.muted}>
            {d.l}
          </text>
        </g>
      ))}

      {/* audit trail */}
      <Panel x={326} y={216} w={220} h={46} label="Audit trail" />
      <text x={338} y={244} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        2026-06-14 14:02 · PLd evidence added
      </text>
      <text x={338} y={255} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        signed · immutable
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Shield ───────────────────────────────────────────────────────────────
function ShieldDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const devices = [
    { id: "amr-01", s: "verified", c: C.green },
    { id: "amr-02", s: "verified", c: C.green },
    { id: "amr-07", s: "rotate key", c: C.amber },
    { id: "amr-12", s: "verified", c: C.green },
  ];
  const cves = [
    { p: "libssl 3.0.2", s: "HIGH", c: C.red },
    { p: "ros-rmw 6.1", s: "MED", c: C.amber },
    { p: "opencv 4.9", s: "LOW", c: C.green },
  ];
  const toggles = ["Secure boot", "Signed OTA", "Encrypted DDS"];
  return (
    <DashboardFrame title="ohho-shield · fleet-security" accent={accent} tools={["48 nodes", "zero-trust"]}>
      {/* risk score */}
      <Panel x={14} y={12} w={150} h={250} label="Risk posture" accent={a} />
      <Ring cx={89} cy={92} r={42} frac={0.86} color={C.green} width={10} label="A−" sub="SECURE" />
      <text x={89} y={158} fontFamily={BODY} fontSize={8.5} fill={C.muted} textAnchor="middle">
        86 / 100
      </text>
      {toggles.map((t, i) => (
        <g key={t}>
          <text x={26} y={188 + i * 24} fontFamily={BODY} fontSize={8.7} fill={C.muted}>
            {t}
          </text>
          <rect x={130} y={180 + i * 24} width={22} height={12} rx={6} fill={C.green} fillOpacity={0.3} stroke={C.green} />
          <circle cx={146} cy={186 + i * 24} r={4} fill={C.green} />
        </g>
      ))}

      {/* device identity */}
      <Panel x={176} y={12} w={190} h={250} label="Device identity" />
      {devices.map((d, i) => (
        <g key={d.id}>
          <rect x={186} y={38 + i * 40} width={170} height={32} rx={6} fill="rgba(255,255,255,0.02)" stroke={C.border} />
          <path d="M198 50 a5 5 0 0 1 10 0 v3 h-10 z M200 53 h6 v8 h-6 z" transform={`translate(-2 ${i * 40})`} fill="none" stroke={d.c} strokeWidth={1.3} />
          <text x={218} y={50 + i * 40} fontFamily={MONO} fontSize={9} fill={C.text}>
            {d.id}
          </text>
          <text x={218} y={62 + i * 40} fontFamily={MONO} fontSize={7} fill={C.faint}>
            sha256·a91f…
          </text>
          <text x={348} y={55 + i * 40} fontFamily={MONO} fontSize={7.5} fill={d.c} textAnchor="end">
            {d.s}
          </text>
        </g>
      ))}

      {/* SBOM / CVE */}
      <Panel x={374} y={12} w={172} h={250} label="SBOM · CVE watch" />
      <text x={384} y={44} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        PACKAGE
      </text>
      <text x={536} y={44} fontFamily={MONO} fontSize={7.5} fill={C.faint} textAnchor="end">
        SEV
      </text>
      <line x1={384} y1={50} x2={536} y2={50} stroke={C.border} />
      {cves.map((c, i) => (
        <g key={c.p}>
          <text x={384} y={72 + i * 26} fontFamily={MONO} fontSize={8.3} fill={C.muted}>
            {c.p}
          </text>
          <rect x={502} y={62 + i * 26} width={34} height={14} rx={4} fill={c.c} fillOpacity={0.18} stroke={c.c} strokeOpacity={0.5} />
          <text x={519} y={72 + i * 26} fontFamily={MONO} fontSize={7} fill={c.c} textAnchor="middle">
            {c.s}
          </text>
        </g>
      ))}
      <line x1={384} y1={148} x2={536} y2={148} stroke={C.border} />
      <text x={384} y={170} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        Packages tracked
      </text>
      <text x={536} y={170} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.text} textAnchor="end">
        312
      </text>
      <text x={384} y={196} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        Open CVEs
      </text>
      <text x={536} y={196} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.amber} textAnchor="end">
        2
      </text>
      <StatusPill x={384} y={214} label="OTA signature OK" color={C.green} />
      <StatusPill x={384} y={236} label="all links encrypted" color={a} />
    </DashboardFrame>
  );
}

// ── OhhO Proof ────────────────────────────────────────────────────────────────
function ProofDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const suites = [
    { l: "Navigation", v: 0.98, c: C.green },
    { l: "Manipulation", v: 0.94, c: C.green },
    { l: "Edge cases", v: 0.87, c: C.amber },
    { l: "Fault injection", v: 0.76, c: C.amber },
  ];
  // deterministic coverage heatmap (0=miss,1=partial,2=pass)
  const cov = [
    [2, 2, 2, 1, 2, 2, 2, 0],
    [2, 2, 1, 2, 2, 2, 1, 2],
    [2, 1, 2, 2, 0, 2, 2, 2],
    [1, 2, 2, 2, 2, 1, 2, 2],
    [2, 2, 2, 1, 2, 2, 2, 1],
  ];
  const heat = (v: number) => (v === 2 ? C.green : v === 1 ? C.amber : C.red);
  const reg = [0.9, 0.92, 0.88, 0.94, 0.93, 0.95, 0.91, 0.96, 0.94, 0.97];
  return (
    <DashboardFrame title="ohho-proof · release-candidate" accent={accent} tools={["12,480 runs", "sim"]}>
      {/* suites */}
      <Panel x={14} y={12} w={196} h={150} label="Suites · pass rate" accent={a} />
      {suites.map((s, i) => (
        <g key={s.l}>
          <text x={24} y={44 + i * 28} fontFamily={BODY} fontSize={9} fill={C.muted}>
            {s.l}
          </text>
          <text x={200} y={44 + i * 28} fontFamily={MONO} fontSize={8.5} fill={s.c} textAnchor="end">
            {Math.round(s.v * 100)}%
          </text>
          <Bar x={24} y={49 + i * 28} w={176} frac={s.v} color={s.c} h={5} />
        </g>
      ))}

      {/* overall + safety case */}
      <Panel x={14} y={172} w={196} h={90} label="Verdict" />
      <Ring cx={56} cy={222} r={30} frac={0.93} color={C.green} width={8} label="93%" />
      <text x={98} y={206} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        Safety case
      </text>
      <StatusPill x={98} y={212} label="ready to ship" color={C.green} />
      <text x={98} y={244} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        Regressions
      </text>
      <text x={186} y={244} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.amber} textAnchor="end">
        1
      </text>

      {/* coverage heatmap */}
      <Panel x={218} y={12} w={328} h={150} label="Scenario coverage" />
      {cov.map((row, r) =>
        row.map((v, c) => (
          <rect
            key={`${r}-${c}`}
            x={230 + c * 37}
            y={40 + r * 22}
            width={32}
            height={17}
            rx={3}
            fill={heat(v)}
            fillOpacity={v === 0 ? 0.5 : 0.22}
            stroke={heat(v)}
            strokeOpacity={0.5}
          />
        ))
      )}
      <text x={230} y={156} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        speed × payload × layout × lighting
      </text>

      {/* regression trend */}
      <Panel x={218} y={172} w={328} h={90} label="Sim-to-real regression" />
      {[0, 1, 2].map((i) => (
        <line key={i} x1={230} y1={210 + i * 16} x2={534} y2={210 + i * 16} stroke={C.grid} />
      ))}
      <path d={sparkPath(reg, 230, 200, 304, 50, true)} fill={a} fillOpacity={0.1} stroke="none" />
      <path d={sparkPath(reg, 230, 200, 304, 50)} fill="none" stroke={a} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
      <text x={230} y={254} fontFamily={MONO} fontSize={7.5} fill={C.faint}>
        build 0142
      </text>
      <text x={534} y={254} fontFamily={MONO} fontSize={7.5} fill={C.green} textAnchor="end">
        0152 · +6%
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Bench ────────────────────────────────────────────────────────────────
function BenchDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const steps = [
    { l: "Frame & base", s: 2 },
    { l: "Mecanum wheels", s: 2 },
    { l: "Motor board", s: 2 },
    { l: "SO-101 arm", s: 2 },
    { l: "Cameras", s: 1 },
    { l: "Compute + power", s: 0 },
  ];
  const tests = [
    { l: "Motors", v: "OK", c: C.green },
    { l: "Encoders", v: "OK", c: C.green },
    { l: "IMU", v: "OK", c: C.green },
    { l: "Arm bus", v: "homing", c: C.amber },
    { l: "Cameras", v: "OK", c: C.green },
  ];
  return (
    <DashboardFrame title="ohho-bench · warehouse-amr" accent={accent} tools={["bring-up", "USB"]}>
      {/* assembly checklist */}
      <Panel x={14} y={12} w={168} h={250} label="Assembly" accent={a} />
      {steps.map((st, i) => (
        <g key={st.l}>
          {st.s === 2 ? (
            <g>
              <circle cx={32} cy={52 + i * 38} r={8} fill={C.green} fillOpacity={0.2} stroke={C.green} />
              <path d={`M28 ${52 + i * 38} l3 3 l5 -6`} fill="none" stroke={C.green} strokeWidth={1.6} strokeLinecap="round" />
            </g>
          ) : st.s === 1 ? (
            <circle cx={32} cy={52 + i * 38} r={8} fill="none" stroke={a} strokeWidth={1.6} />
          ) : (
            <circle cx={32} cy={52 + i * 38} r={8} fill="none" stroke={C.faint} strokeWidth={1.4} strokeDasharray="2 2" />
          )}
          <text x={48} y={49 + i * 38} fontFamily={BODY} fontSize={9.5} fill={st.s === 0 ? C.faint : C.text}>
            {st.l}
          </text>
          <text x={48} y={61 + i * 38} fontFamily={MONO} fontSize={7.5} fill={st.s === 1 ? a : C.faint}>
            {st.s === 2 ? "done" : st.s === 1 ? "in progress" : "pending"}
          </text>
        </g>
      ))}

      {/* wiring / port map */}
      <Panel x={194} y={12} w={206} h={250} label="Wiring · port map" />
      <rect x={272} y={120} width={50} height={34} rx={6} fill={C.panelHi} stroke={a} />
      <text x={297} y={141} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="middle">
        compute
      </text>
      {/* peripheral boxes */}
      {[
        { x: 210, y: 44, l: "motor board", p: "USB0 · 115200" },
        { x: 334, y: 44, l: "arm bus", p: "ACM0 · 1 Mbd" },
        { x: 210, y: 200, l: "cameras", p: "USB · uvc" },
        { x: 334, y: 200, l: "battery", p: "12 V · pwr" },
      ].map((b) => (
        <g key={b.l}>
          <rect x={b.x} y={b.y} width={56} height={30} rx={5} fill={C.surfHi} stroke={C.borderHi} />
          <text x={b.x + 28} y={b.y + 14} fontFamily={MONO} fontSize={7.5} fill={C.text} textAnchor="middle">
            {b.l}
          </text>
          <text x={b.x + 28} y={b.y + 24} fontFamily={MONO} fontSize={6.5} fill={C.faint} textAnchor="middle">
            {b.p}
          </text>
        </g>
      ))}
      <line x1={238} y1={74} x2={285} y2={120} stroke={a} strokeOpacity={0.55} />
      <line x1={362} y1={74} x2={309} y2={120} stroke={a} strokeOpacity={0.55} />
      <line x1={238} y1={200} x2={285} y2={154} stroke={a} strokeOpacity={0.55} />
      <line x1={362} y1={200} x2={309} y2={154} stroke={C.green} strokeOpacity={0.55} />

      {/* self-test */}
      <Panel x={408} y={12} w={138} h={150} label="Self-test" />
      {tests.map((t, i) => (
        <g key={t.l}>
          <Dot cx={420} cy={43 + i * 22} r={2.6} color={t.c} />
          <text x={430} y={46 + i * 22} fontFamily={BODY} fontSize={8.7} fill={C.muted}>
            {t.l}
          </text>
          <text x={536} y={46 + i * 22} fontFamily={MONO} fontSize={7.5} fill={t.c} textAnchor="end">
            {t.v}
          </text>
        </g>
      ))}

      {/* calibration */}
      <Panel x={408} y={172} w={138} h={90} label="Calibration" accent={a} />
      <Ring cx={444} cy={222} r={26} frac={0.8} color={a} width={7} label="80%" />
      <text x={482} y={208} fontFamily={BODY} fontSize={8.3} fill={C.muted}>
        odometry ✓
      </text>
      <text x={482} y={222} fontFamily={BODY} fontSize={8.3} fill={C.muted}>
        IMU bias ✓
      </text>
      <text x={482} y={236} fontFamily={BODY} fontSize={8.3} fill={C.faint}>
        BEV rig …
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Train ────────────────────────────────────────────────────────────────
function TrainDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const loss = [0.92, 0.78, 0.63, 0.54, 0.45, 0.38, 0.33, 0.29, 0.25, 0.22, 0.19, 0.17];
  const succ = [0.2, 0.28, 0.35, 0.44, 0.5, 0.58, 0.64, 0.69, 0.74, 0.78, 0.82, 0.86];
  const cfg = [
    { l: "method", v: "smolvla" },
    { l: "dataset", v: "1,043 eps" },
    { l: "epochs", v: "40 / 50" },
    { l: "lr", v: "1e-4" },
    { l: "device", v: "cuda:0" },
  ];
  return (
    <DashboardFrame title="ohho-train · smolvla-ft" accent={accent} tools={["W&B", "GPU 0"]}>
      {/* run config */}
      <Panel x={14} y={12} w={150} h={250} label="Run config" accent={a} />
      {cfg.map((c, i) => (
        <g key={c.l}>
          <text x={26} y={48 + i * 26} fontFamily={MONO} fontSize={8.5} fill={C.faint}>
            {c.l}
          </text>
          <text x={152} y={48 + i * 26} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">
            {c.v}
          </text>
          <line x1={26} y1={56 + i * 26} x2={152} y2={56 + i * 26} stroke={C.grid} />
        </g>
      ))}
      <rect x={26} y={196} width={126} height={26} rx={6} fill="rgba(124,58,237,0.10)" stroke={C.violetLite} strokeOpacity={0.5} />
      <text x={89} y={212} fontFamily={BODY} fontSize={8.5} fill={C.violetLite} textAnchor="middle">
        OmniVLA engine
      </text>
      <StatusPill x={26} y={232} label="training" color={C.green} />

      {/* loss / success chart */}
      <Panel x={176} y={12} w={224} h={150} label="Loss · success rate" accent={a} />
      <text x={388} y={30} fontFamily={DISPLAY} fontSize={14} fontWeight={700} fill={C.text} textAnchor="end">
        86%
      </text>
      {[0, 1, 2].map((i) => (
        <line key={i} x1={188} y1={56 + i * 30} x2={388} y2={56 + i * 30} stroke={C.grid} />
      ))}
      <path d={sparkPath(loss, 188, 48, 200, 92, true)} fill={a} fillOpacity={0.1} stroke="none" />
      <path d={sparkPath(loss, 188, 48, 200, 92)} fill="none" stroke={a} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
      <path d={sparkPath(succ, 188, 48, 200, 92)} fill="none" stroke={C.green} strokeWidth={2} strokeDasharray="4 3" strokeLinecap="round" strokeLinejoin="round" />
      <Dot cx={196} cy={150} r={2.6} color={a} />
      <text x={204} y={153} fontFamily={MONO} fontSize={7.5} fill={C.faint}>loss</text>
      <Dot cx={244} cy={150} r={2.6} color={C.green} />
      <text x={252} y={153} fontFamily={MONO} fontSize={7.5} fill={C.faint}>success</text>

      {/* verify / export */}
      <Panel x={176} y={172} w={224} h={90} label="Verify & export" />
      <text x={188} y={200} fontFamily={BODY} fontSize={8.7} fill={C.muted}>
        Best-of-N · safety checks
      </text>
      <StatusPill x={330} y={190} label="passed" color={C.green} />
      <rect x={188} y={216} width={200} height={30} rx={7} fill={a} />
      <text x={288} y={235} fontFamily={BODY} fontSize={9.5} fontWeight={700} fill={C.bg} textAnchor="middle">
        Export → OhhO Serve
      </text>

      {/* GPU / steps */}
      <Panel x={412} y={12} w={134} h={250} label="Run" />
      <Ring cx={479} cy={84} r={34} frac={0.81} color={a} width={9} label="81%" sub="GPU" />
      <text x={428} y={146} fontFamily={BODY} fontSize={8.7} fill={C.muted}>step</text>
      <text x={532} y={146} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">38.4k</text>
      <Bar x={428} y={152} w={104} frac={0.8} color={a} h={4} />
      <text x={428} y={176} fontFamily={BODY} fontSize={8.7} fill={C.muted}>VRAM</text>
      <text x={532} y={176} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">14.2 / 16</text>
      <Bar x={428} y={182} w={104} frac={0.89} color={C.amber} h={4} />
      <text x={428} y={206} fontFamily={BODY} fontSize={8.7} fill={C.muted}>ETA</text>
      <text x={532} y={206} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">12m</text>
      <StatusPill x={428} y={222} label="W&B synced" color={C.green} />
      <StatusPill x={428} y={242} label="checkpoint 0040" color={a} />
    </DashboardFrame>
  );
}

// ── OhhO Autonomy ─────────────────────────────────────────────────────────────
function AutonomyDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const mission = [
    { l: "navigate · kitchen", s: 2 },
    { l: "detect · red cup", s: 1 },
    { l: "pick · cup", s: 0 },
    { l: "navigate · bench", s: 0 },
  ];
  const modes = [
    { l: "nav2", on: true },
    { l: "vla", on: false },
    { l: "rl_nav", on: false },
    { l: "teleop", on: false },
  ];
  return (
    <DashboardFrame title="ohho-autonomy · mission" accent={accent} tools={["SLAM", "Nav2"]}>
      {/* mission state machine */}
      <Panel x={14} y={12} w={156} h={250} label="Mission" accent={a} />
      {mission.map((m, i) => (
        <g key={m.l}>
          <line x1={28} y1={48 + i * 36} x2={28} y2={i === mission.length - 1 ? 48 + i * 36 : 84 + i * 36} stroke={C.border} />
          {m.s === 2 ? (
            <circle cx={28} cy={48 + i * 36} r={6} fill={C.green} />
          ) : m.s === 1 ? (
            <circle cx={28} cy={48 + i * 36} r={6} fill={a} />
          ) : (
            <circle cx={28} cy={48 + i * 36} r={6} fill={C.surf} stroke={C.faint} />
          )}
          <text x={42} y={45 + i * 36} fontFamily={BODY} fontSize={9} fill={m.s === 0 ? C.faint : C.text}>
            {m.l}
          </text>
          <text x={42} y={56 + i * 36} fontFamily={MONO} fontSize={7} fill={m.s === 1 ? a : C.faint}>
            {m.s === 2 ? "done" : m.s === 1 ? "running" : "queued"}
          </text>
        </g>
      ))}
      {/* agent prompt */}
      <rect x={24} y={200} width={136} height={52} rx={7} fill="#0A0F1C" stroke={C.violetLite} strokeOpacity={0.4} />
      <text x={32} y={216} fontFamily={MONO} fontSize={6.5} fill={C.violetLite}>
        ✦ AGENT · claude
      </text>
      <text x={32} y={230} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        “take the red cup to
      </text>
      <text x={32} y={241} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        the bench”
      </text>

      {/* SLAM map */}
      <Panel x={180} y={12} w={232} h={250} label="SLAM map · localized" />
      <rect x={190} y={36} width={212} height={216} rx={6} fill="#0A1424" stroke={C.border} />
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <line key={`gh${i}`} x1={190} y1={36 + i * 36} x2={402} y2={36 + i * 36} stroke={C.grid} />
      ))}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <line key={`gv${i}`} x1={190 + i * 36} y1={36} x2={190 + i * 36} y2={252} stroke={C.grid} />
      ))}
      {/* walls / occupancy */}
      <path d="M206 60 h120 v14 h-120 z" fill="rgba(255,255,255,0.08)" />
      <path d="M340 60 v120 h14 v-120 z" fill="rgba(255,255,255,0.08)" />
      <path d="M206 210 h90 v14 h-90 z" fill="rgba(255,255,255,0.08)" />
      {/* costmap obstacle */}
      <circle cx={300} cy={150} r={14} fill={C.amber} fillOpacity={0.18} stroke={C.amber} strokeDasharray="3 3" />
      {/* planned path */}
      <path d="M224 226 C 250 180, 232 130, 270 110 S 320 80, 332 92" fill="none" stroke={a} strokeWidth={2.4} strokeDasharray="6 4" strokeLinecap="round" />
      {/* waypoints */}
      <circle cx={270} cy={110} r={3} fill={C.violetLite} />
      {/* goal */}
      <g>
        <circle cx={332} cy={92} r={6} fill="none" stroke={C.green} strokeWidth={1.6} />
        <circle cx={332} cy={92} r={2} fill={C.green} />
      </g>
      {/* robot */}
      <rect x={216} y={218} width={16} height={16} rx={3} fill={C.panelHi} stroke={a} strokeWidth={1.6} />
      <path d="M224 222 v6" stroke={a} strokeWidth={2} strokeLinecap="round" />
      <text x={196} y={250} fontFamily={MONO} fontSize={7} fill={C.faint}>
        kitchen → bench
      </text>

      {/* behavior / mux */}
      <Panel x={420} y={12} w={126} h={150} label="Control mode" accent={a} />
      {modes.map((m, i) => (
        <g key={m.l}>
          <rect x={430} y={40 + i * 26} width={106} height={20} rx={5} fill={m.on ? "rgba(124,58,237,0.14)" : "rgba(255,255,255,0.02)"} stroke={m.on ? a : C.border} />
          <Dot cx={442} cy={50 + i * 26} r={2.6} color={m.on ? a : C.faint} />
          <text x={452} y={53 + i * 26} fontFamily={MONO} fontSize={8.5} fill={m.on ? C.text : C.muted}>
            {m.l}
          </text>
          {m.on && (
            <text x={528} y={53 + i * 26} fontFamily={MONO} fontSize={7} fill={a} textAnchor="end">
              active
            </text>
          )}
        </g>
      ))}

      {/* nav status */}
      <Panel x={420} y={172} w={126} h={90} label="Nav2" />
      <StatusPill x={430} y={196} label="navigating" color={C.green} />
      <text x={430} y={226} fontFamily={BODY} fontSize={8.5} fill={C.muted}>dist to goal</text>
      <text x={536} y={226} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">2.4 m</text>
      <text x={430} y={244} fontFamily={BODY} fontSize={8.5} fill={C.muted}>ETA</text>
      <text x={536} y={244} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">9 s</text>
    </DashboardFrame>
  );
}

// ── OhhO Mind ───────────────────────────────────────────────────────────────
function MindDash({ accent }: DashProps) {
  const a = accentHex(accent);
  // s: 2 = done, 1 = active, 0 = pending
  const loop = [
    { l: "perceive", s: 2 },
    { l: "reason", s: 1 },
    { l: "verify", s: 0 },
    { l: "act", s: 0 },
    { l: "monitor", s: 0 },
    { l: "reflect", s: 0 },
    { l: "remember", s: 0 },
  ];
  const brains = [
    { l: "cloud · claude", on: true },
    { l: "on-device llm", on: false },
    { l: "deepx npu", on: false },
  ];
  const state = [
    { l: "base pose", v: "1.8, 0.4, 0°" },
    { l: "arm", v: "home · open" },
    { l: "nearest", v: "red_cup 0.42 m" },
    { l: "mission", v: "active" },
  ];
  return (
    <DashboardFrame title="ohho-mind · agent loop" accent={accent} tools={["GOAL", "OTA"]}>
      {/* cognitive loop */}
      <Panel x={14} y={12} w={150} h={250} label="Agent loop" accent={a} />
      {loop.map((p, i) => {
        const cy = 44 + i * 27;
        const last = i === loop.length - 1;
        return (
          <g key={p.l}>
            {!last && <line x1={28} y1={cy} x2={28} y2={cy + 27} stroke={C.border} />}
            {p.s === 2 ? (
              <circle cx={28} cy={cy} r={5.5} fill={C.green} />
            ) : p.s === 1 ? (
              <circle cx={28} cy={cy} r={5.5} fill={a} />
            ) : (
              <circle cx={28} cy={cy} r={5.5} fill={C.surf} stroke={C.faint} />
            )}
            <text x={42} y={cy + 3.5} fontFamily={BODY} fontSize={9.5} fill={p.s === 0 ? C.faint : C.text}>
              {p.l}
            </text>
            {p.s === 1 && (
              <text x={154} y={cy + 3.5} fontFamily={MONO} fontSize={7} fill={a} textAnchor="end">
                now
              </text>
            )}
          </g>
        );
      })}
      <text x={24} y={252} fontFamily={MONO} fontSize={6.5} fill={C.faint}>
        monitor ↻ perceive
      </text>

      {/* goal */}
      <rect x={176} y={12} width={236} height={34} rx={7} fill="#0A0F1C" stroke={a} strokeOpacity={0.4} />
      <text x={186} y={26} fontFamily={MONO} fontSize={6.5} fill={a}>
        ✦ GOAL
      </text>
      <text x={186} y={39} fontFamily={BODY} fontSize={9} fill={C.text}>
        “find the red cup and bring it back”
      </text>

      {/* world state */}
      <Panel x={176} y={54} w={236} h={120} label="World state · /agent/world_state" />
      {state.map((s, i) => (
        <g key={s.l}>
          <text x={188} y={88 + i * 22} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
            {s.l}
          </text>
          <text x={400} y={88 + i * 22} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">
            {s.v}
          </text>
          {i < state.length - 1 && <line x1={188} y1={94 + i * 22} x2={400} y2={94 + i * 22} stroke={C.grid} />}
        </g>
      ))}

      {/* verified next action */}
      <Panel x={176} y={182} w={236} h={80} label="Next action · verified" accent={a} />
      <text x={188} y={208} fontFamily={MONO} fontSize={9} fill={C.text}>
        run_skill(
      </text>
      <text x={196} y={221} fontFamily={MONO} fontSize={9} fill={a}>
        rl_arm, “pick up the cup”
      </text>
      <text x={188} y={234} fontFamily={MONO} fontSize={9} fill={C.text}>
        )
      </text>
      <text x={188} y={252} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        plan confidence
      </text>
      <Bar x={290} y={247} w={110} frac={0.86} color={C.green} />

      {/* reasoning router */}
      <Panel x={420} y={12} w={126} h={120} label="Reasoning" accent={a} />
      {brains.map((b, i) => (
        <g key={b.l}>
          <rect
            x={430}
            y={40 + i * 28}
            width={106}
            height={22}
            rx={5}
            fill={b.on ? "rgba(124,58,237,0.14)" : "rgba(255,255,255,0.02)"}
            stroke={b.on ? a : C.border}
          />
          <Dot cx={442} cy={51 + i * 28} r={2.6} color={b.on ? a : C.faint} />
          <text x={452} y={54 + i * 28} fontFamily={MONO} fontSize={8} fill={b.on ? C.text : C.muted}>
            {b.l}
          </text>
          <text x={528} y={54 + i * 28} fontFamily={MONO} fontSize={7} fill={b.on ? a : C.faint} textAnchor="end">
            {b.on ? "active" : "ready"}
          </text>
        </g>
      ))}

      {/* safety gate + learning */}
      <Panel x={420} y={142} w={126} h={120} label="Safety gate" />
      <Ring cx={452} cy={196} r={23} frac={1} color={C.green} width={7} label="✓" sub="verified" />
      <text x={486} y={186} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        base ≤ 0.20
      </text>
      <text x={486} y={199} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        arm ≤ 0.05
      </text>
      <text x={486} y={212} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        limits ok
      </text>
      <line x1={430} y1={230} x2={536} y2={230} stroke={C.grid} />
      <text x={430} y={248} fontFamily={BODY} fontSize={8} fill={C.muted}>
        episodes → learn
      </text>
      <text x={536} y={248} fontFamily={MONO} fontSize={8} fill={a} textAnchor="end">
        128
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Connect ─────────────────────────────────────────────────────────────
function ConnectDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const protos = [
    { l: "Wi-Fi · ROSBridge", on: true, c: C.green },
    { l: "USB · Web Serial", on: true, c: C.green },
    { l: "Bluetooth · BLE", on: false, c: C.faint },
    { l: "Simulated", on: true, c: C.green },
  ];
  const telemetry = [
    { l: "odom", v: "1.8, 0.4, 12°" },
    { l: "joints[6]", v: "0.0, 0.1, …" },
    { l: "battery", v: "0.82" },
    { l: "msg rate", v: "48 / s" },
  ];
  return (
    <DashboardFrame title="ohho-connect · garage" accent={accent} tools={["Wi-Fi", "live"]}>
      {/* protocol picker */}
      <Panel x={14} y={12} w={170} h={250} label="Protocols" accent={a} />
      {protos.map((p, i) => (
        <g key={p.l}>
          <rect x={22} y={38 + i * 38} width={154} height={30} rx={6} fill={p.on ? "rgba(255,255,255,0.03)" : "rgba(255,255,255,0.01)"} stroke={p.on ? a : C.border} />
          <Dot cx={34} cy={53 + i * 38} r={3} color={p.c} />
          <text x={44} y={56 + i * 38} fontFamily={BODY} fontSize={9} fill={p.on ? C.text : C.muted}>
            {p.l}
          </text>
          <text x={166} y={56 + i * 38} fontFamily={MONO} fontSize={7} fill={p.on ? p.c : C.faint} textAnchor="end">
            {p.on ? "ready" : "n/a"}
          </text>
        </g>
      ))}

      {/* live link status */}
      <Panel x={194} y={12} w={218} h={120} label="Link status" accent={a} />
      <Ring cx={248} cy={78} r={32} frac={0.92} color={C.green} width={8} label="28ms" sub="LATENCY" />
      <text x={296} y={56} fontFamily={BODY} fontSize={9} fill={C.muted}>state</text>
      <text x={400} y={56} fontFamily={MONO} fontSize={9} fill={C.green} textAnchor="end">connected</text>
      <text x={296} y={76} fontFamily={BODY} fontSize={9} fill={C.muted}>uptime</text>
      <text x={400} y={76} fontFamily={MONO} fontSize={9} fill={C.text} textAnchor="end">12m 04s</text>
      <text x={296} y={96} fontFamily={BODY} fontSize={9} fill={C.muted}>msg/s</text>
      <text x={400} y={96} fontFamily={MONO} fontSize={9} fill={a} textAnchor="end">48</text>
      <Bar x={296} y={104} w={104} frac={0.48} color={a} h={4} />

      {/* telemetry stream */}
      <Panel x={194} y={142} w={218} h={120} label="Telemetry stream" />
      {telemetry.map((t, i) => (
        <g key={t.l}>
          <text x={206} y={172 + i * 22} fontFamily={MONO} fontSize={8.5} fill={C.faint}>
            {t.l}
          </text>
          <text x={400} y={172 + i * 22} fontFamily={MONO} fontSize={8.5} fill={C.text} textAnchor="end">
            {t.v}
          </text>
          {i < telemetry.length - 1 && <line x1={206} y1={178 + i * 22} x2={400} y2={178 + i * 22} stroke={C.grid} />}
        </g>
      ))}

      {/* e-stop */}
      <Panel x={422} y={12} w={124} h={250} label="Safety" />
      <rect x={434} y={44} width={100} height={32} rx={7} fill="rgba(248,113,113,0.14)" stroke={C.red} />
      <text x={484} y={64} fontFamily={DISPLAY} fontSize={11} fontWeight={700} fill={C.red} textAnchor="middle">
        E-STOP
      </text>
      <text x={434} y={96} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        velocity clamp
      </text>
      <text x={534} y={96} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="end">
        0.2 m/s
      </text>
      <text x={434} y={116} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        angular clamp
      </text>
      <text x={534} y={116} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="end">
        1.0 rad/s
      </text>
      <StatusPill x={434} y={132} label="auto-reconnect" color={C.green} />
      <StatusPill x={434} y={154} label="mixed-content OK" color={C.green} />
      <line x1={434} y1={184} x2={534} y2={184} stroke={C.grid} />
      <text x={434} y={204} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        robot
      </text>
      <text x={534} y={204} fontFamily={MONO} fontSize={8} fill={a} textAnchor="end">
        omnibot
      </text>
      <text x={434} y={224} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        garage ID
      </text>
      <text x={534} y={224} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="end">
        r-0148
      </text>
      <text x={434} y={244} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        protocol
      </text>
      <text x={534} y={244} fontFamily={MONO} fontSize={8} fill={a} textAnchor="end">
        rosbridge
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Bridge ──────────────────────────────────────────────────────────────
function BridgeDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const native = [
    { l: "LowState.motor[20]", v: "q, dq, tau" },
    { l: "LowState.imu", v: "quat, gyro" },
    { l: "HighCmd", v: "vel, pose" },
  ];
  const ros = [
    { l: "/joint_states", v: "JointState" },
    { l: "/imu/data", v: "Imu" },
    { l: "/odom", v: "Odometry" },
    { l: "/cmd_vel", v: "Twist" },
  ];
  return (
    <DashboardFrame title="ohho-bridge · unitree-g1" accent={accent} tools={["DDS", "ROS 2"]}>
      {/* native protocol */}
      <Panel x={14} y={12} w={150} h={250} label="Unitree DDS" />
      {native.map((n, i) => (
        <g key={n.l}>
          <rect x={22} y={40 + i * 44} width={134} height={34} rx={6} fill="rgba(255,255,255,0.02)" stroke={C.border} />
          <text x={30} y={56 + i * 44} fontFamily={MONO} fontSize={7.5} fill={C.text}>
            {n.l}
          </text>
          <text x={30} y={68 + i * 44} fontFamily={MONO} fontSize={7} fill={C.faint}>
            {n.v}
          </text>
        </g>
      ))}
      <text x={22} y={186} fontFamily={MONO} fontSize={7} fill={C.faint}>
        sdk: unitree_sdk2
      </text>
      <text x={22} y={198} fontFamily={MONO} fontSize={7} fill={C.faint}>
        dds: cyclone
      </text>
      <text x={22} y={210} fontFamily={MONO} fontSize={7} fill={C.faint}>
        model: G1 (29 DoF)
      </text>

      {/* bridge diagram */}
      <Panel x={174} y={12} w={210} h={250} label="Bridge" accent={a} />
      {/* left box */}
      <rect x={186} y={80} width={56} height={40} rx={7} fill={C.panelHi} stroke={C.borderHi} />
      <text x={214} y={97} fontFamily={MONO} fontSize={7.5} fill={C.text} textAnchor="middle">
        DDS
      </text>
      <text x={214} y={110} fontFamily={MONO} fontSize={6.5} fill={C.faint} textAnchor="middle">
        native
      </text>
      {/* right box */}
      <rect x={316} y={80} width={56} height={40} rx={7} fill={C.panelHi} stroke={a} />
      <text x={344} y={97} fontFamily={MONO} fontSize={7.5} fill={a} textAnchor="middle">
        ROS 2
      </text>
      <text x={344} y={110} fontFamily={MONO} fontSize={6.5} fill={C.faint} textAnchor="middle">
        topics
      </text>
      {/* arrows */}
      <line x1={242} y1={90} x2={316} y2={90} stroke={a} strokeWidth={1.6} markerEnd="url(#ba)" />
      <line x1={316} y1={110} x2={242} y2={110} stroke={C.green} strokeWidth={1.6} />
      <text x={279} y={85} fontFamily={MONO} fontSize={6.5} fill={a} textAnchor="middle">
        state →
      </text>
      <text x={279} y={124} fontFamily={MONO} fontSize={6.5} fill={C.green} textAnchor="middle">
        ← cmd
      </text>
      {/* joint map */}
      <text x={186} y={146} fontFamily={MONO} fontSize={7} fill={C.faint}>
        JOINT INDEX MAP
      </text>
      {["0: hip_pitch_L", "5: knee_L", "12: shoulder_L", "20: wrist_R"].map((j, i) => (
        <text key={j} x={186} y={162 + i * 14} fontFamily={MONO} fontSize={7.5} fill={i === 1 ? a : C.muted}>
          {j}
        </text>
      ))}
      {/* gain defaults */}
      <text x={186} y={222} fontFamily={MONO} fontSize={7} fill={C.faint}>
        IMPEDANCE DEFAULTS
      </text>
      <text x={186} y={238} fontFamily={MONO} fontSize={7.5} fill={C.muted}>
        kp: 80 · kd: 3 (leg)
      </text>
      <text x={186} y={252} fontFamily={MONO} fontSize={7.5} fill={C.muted}>
        kp: 40 · kd: 2 (arm)
      </text>

      {/* ROS topics out */}
      <Panel x={394} y={12} w={152} h={250} label="ROS 2 topics" accent={a} />
      {ros.map((r, i) => (
        <g key={r.l}>
          <rect x={402} y={40 + i * 38} width={136} height={30} rx={6} fill="rgba(255,255,255,0.02)" stroke={C.border} />
          <text x={410} y={55 + i * 38} fontFamily={MONO} fontSize={8} fill={a}>
            {r.l}
          </text>
          <text x={410} y={65 + i * 38} fontFamily={MONO} fontSize={6.5} fill={C.faint}>
            {r.v}
          </text>
        </g>
      ))}
      <StatusPill x={402} y={208} label="bridge active" color={C.green} />
      <text x={402} y={238} fontFamily={MONO} fontSize={7} fill={C.faint}>
        latency: 2.1 ms
      </text>
      <text x={402} y={252} fontFamily={MONO} fontSize={7} fill={C.faint}>
        jitter: 0.3 ms
      </text>
    </DashboardFrame>
  );
}

// ── OhhO Market ──────────────────────────────────────────────────────────────
function MarketDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const skills = [
    { l: "pick-place-cup", r: "G1", s: 0.94, p: "$49", c: C.green },
    { l: "patrol-warehouse", r: "Go2", s: 0.97, p: "$29", c: C.green },
    { l: "weld-seam-v2", r: "UR5e", s: 0.89, p: "$99", c: C.amber },
  ];
  const proof = [
    { l: "Navigation", v: 0.96 },
    { l: "Manipulation", v: 0.94 },
    { l: "Edge cases", v: 0.87 },
  ];
  return (
    <DashboardFrame title="ohho-market · skills" accent={accent} tools={["G1", "verified"]}>
      {/* skill listings */}
      <Panel x={14} y={12} w={210} h={250} label="Skills" accent={a} />
      {skills.map((s, i) => (
        <g key={s.l}>
          <rect x={22} y={40 + i * 62} width={194} height={52} rx={7} fill={i === 0 ? "rgba(0,212,255,0.06)" : "rgba(255,255,255,0.02)"} stroke={i === 0 ? a : C.border} />
          <text x={30} y={56 + i * 62} fontFamily={BODY} fontSize={9} fill={C.text} fontWeight={600}>
            {s.l}
          </text>
          <rect x={30} y={62 + i * 62} width={24} height={12} rx={3} fill={s.c} fillOpacity={0.18} stroke={s.c} strokeOpacity={0.5} />
          <text x={42} y={71 + i * 62} fontFamily={MONO} fontSize={6.5} fill={s.c} textAnchor="middle">
            {s.r}
          </text>
          <text x={170} y={56 + i * 62} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="end">
            {s.p}
          </text>
          <text x={170} y={68 + i * 62} fontFamily={MONO} fontSize={7} fill={s.c} textAnchor="end">
            {Math.round(s.s * 100)}% pass
          </text>
          <Bar x={60} y={66 + i * 62} w={100} frac={s.s} color={s.c} h={3} />
        </g>
      ))}

      {/* skill detail */}
      <Panel x={234} y={12} w={180} h={170} label="pick-place-cup · detail" accent={a} />
      <text x={246} y={40} fontFamily={MONO} fontSize={7} fill={C.faint}>
        ROBOT
      </text>
      <text x={402} y={40} fontFamily={MONO} fontSize={7.5} fill={a} textAnchor="end">
        Unitree G1
      </text>
      <text x={246} y={56} fontFamily={MONO} fontSize={7} fill={C.faint}>
        METHOD
      </text>
      <text x={402} y={56} fontFamily={MONO} fontSize={7.5} fill={C.text} textAnchor="end">
        SmolVLA
      </text>
      <text x={246} y={72} fontFamily={MONO} fontSize={7} fill={C.faint}>
        DATA
      </text>
      <text x={402} y={72} fontFamily={MONO} fontSize={7.5} fill={C.text} textAnchor="end">
        1,043 eps
      </text>
      <line x1={246} y1={80} x2={402} y2={80} stroke={C.grid} />
      <text x={246} y={96} fontFamily={MONO} fontSize={7} fill={C.faint}>
        PROOF VERIFICATION
      </text>
      {proof.map((p, i) => (
        <g key={p.l}>
          <text x={246} y={114 + i * 18} fontFamily={BODY} fontSize={8} fill={C.muted}>
            {p.l}
          </text>
          <Bar x={330} y={108 + i * 18} w={72} frac={p.v} color={p.v > 0.9 ? C.green : C.amber} h={4} />
          <text x={402} y={114 + i * 18} fontFamily={MONO} fontSize={7} fill={p.v > 0.9 ? C.green : C.amber} textAnchor="end">
            {Math.round(p.v * 100)}%
          </text>
        </g>
      ))}
      <StatusPill x={246} y={160} label="signed · Shield" color={C.green} />

      {/* deploy */}
      <Panel x={234} y={192} w={180} h={70} label="Deploy" />
      <rect x={246} y={214} width={156} height={26} rx={7} fill={a} />
      <text x={324} y={231} fontFamily={BODY} fontSize={9} fontWeight={700} fill={C.bg} textAnchor="middle">
        Deploy → OhhO Serve
      </text>
      <text x={246} y={254} fontFamily={MONO} fontSize={7} fill={C.faint}>
        or push via OhhO Fleet OTA
      </text>

      {/* author + stats */}
      <Panel x={424} y={12} w={122} h={250} label="Author" />
      <circle cx={456} cy={50} r={16} fill={C.panelHi} stroke={a} />
      <text x={456} y={54} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={a} textAnchor="middle">
        RL
      </text>
      <text x={436} y={84} fontFamily={BODY} fontSize={8.5} fill={C.text}>
        robotics-lab
      </text>
      <text x={436} y={98} fontFamily={MONO} fontSize={7} fill={C.faint}>
        12 skills · 4.9 ★
      </text>
      <line x1={436} y1={110} x2={534} y2={110} stroke={C.grid} />
      <text x={436} y={128} fontFamily={MONO} fontSize={7} fill={C.faint}>
        DEPLOYS
      </text>
      <text x={534} y={128} fontFamily={DISPLAY} fontSize={13} fontWeight={700} fill={C.text} textAnchor="end">
        1,248
      </text>
      <text x={436} y={150} fontFamily={MONO} fontSize={7} fill={C.faint}>
        REVENUE
      </text>
      <text x={534} y={150} fontFamily={DISPLAY} fontSize={13} fontWeight={700} fill={C.green} textAnchor="end">
        $4,820
      </text>
      <line x1={436} y1={164} x2={534} y2={164} stroke={C.grid} />
      <text x={436} y={184} fontFamily={MONO} fontSize={7} fill={C.faint}>
        TAKE RATE
      </text>
      <text x={534} y={184} fontFamily={MONO} fontSize={8} fill={C.text} textAnchor="end">
        15%
      </text>
      <text x={436} y={206} fontFamily={MONO} fontSize={7} fill={C.faint}>
        VERSION
      </text>
      <text x={534} y={206} fontFamily={MONO} fontSize={8} fill={a} textAnchor="end">
        v2.1.0
      </text>
      <StatusPill x={436} y={224} label="verified" color={C.green} />
      <StatusPill x={436} y={244} label="cross-brand" color={a} />
    </DashboardFrame>
  );
}

// ── OhhO Twin ────────────────────────────────────────────────────────────────
function TwinDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const replay = [0.5, 0.55, 0.48, 0.6, 0.52, 0.65, 0.58, 0.7, 0.62, 0.55, 0.68, 0.6];
  const predictions = [
    { l: "motor temp", v: "62°C → 78°C", c: C.amber },
    { l: "battery", v: "0.82 → 0.61", c: C.green },
    { l: "joint wear", v: "MTBF 412h", c: C.amber },
  ];
  return (
    <DashboardFrame title="ohho-twin · warehouse-amr" accent={accent} tools={["Isaac Sim", "live"]}>
      {/* sim world */}
      <Panel x={14} y={12} w={240} h={170} label="Isaac Sim · live mirror" accent={a} />
      <rect x={24} y={36} width={220} height={134} rx={6} fill="#0A1424" stroke={C.border} />
      {/* floor grid */}
      {[0, 1, 2, 3].map((i) => (
        <line key={`th${i}`} x1={24} y1={70 + i * 28} x2={244} y2={70 + i * 28} stroke={C.grid} />
      ))}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <line key={`tv${i}`} x1={60 + i * 36} y1={36} x2={60 + i * 36} y2={170} stroke={C.grid} />
      ))}
      {/* shelves */}
      <rect x={40} y={44} width={50} height={10} rx={2} fill="rgba(255,255,255,0.08)" />
      <rect x={180} y={44} width={50} height={10} rx={2} fill="rgba(255,255,255,0.08)" />
      <rect x={40} y={150} width={50} height={10} rx={2} fill="rgba(255,255,255,0.08)" />
      {/* real robot (live) */}
      <rect x={120} y={100} width={20} height={20} rx={4} fill={C.panelHi} stroke={a} strokeWidth={1.6} />
      <path d="M130 106 v6" stroke={a} strokeWidth={2} strokeLinecap="round" />
      <circle cx={130} cy={110} r={24} fill="none" stroke={a} strokeOpacity={0.2} strokeDasharray="3 3" />
      {/* twin robot (sim) */}
      <rect x={170} y={80} width={20} height={20} rx={4} fill="none" stroke={C.violetLite} strokeWidth={1.4} strokeDasharray="3 2" />
      <text x={28} y={176} fontFamily={MONO} fontSize={6.5} fill={C.faint}>
        ● real &nbsp; ⋯ sim mirror
      </text>

      {/* replay timeline */}
      <Panel x={14} y={192} w={240} h={70} label="Replay timeline" />
      <line x1={26} y1={232} x2={242} y2={232} stroke={C.track} strokeWidth={4} strokeLinecap="round" />
      <line x1={26} y1={232} x2={160} y2={232} stroke={a} strokeWidth={4} strokeLinecap="round" />
      <circle cx={160} cy={232} r={5} fill={C.text} stroke={a} strokeWidth={2} />
      <text x={26} y={254} fontFamily={MONO} fontSize={7} fill={C.faint}>
        00:00
      </text>
      <text x={242} y={254} fontFamily={MONO} fontSize={7} fill={C.faint} textAnchor="end">
        01:12:04
      </text>

      {/* what-if branch */}
      <Panel x={264} y={12} w={140} h={150} label="What-if" accent={a} />
      <text x={276} y={38} fontFamily={MONO} fontSize={7} fill={C.faint}>
        BRANCH FROM 00:34:12
      </text>
      <text x={276} y={56} fontFamily={BODY} fontSize={8.5} fill={C.text}>
        different grasp angle
      </text>
      <text x={276} y={68} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        +15° approach
      </text>
      <rect x={276} y={80} width={116} height={28} rx={6} fill={a} />
      <text x={334} y={98} fontFamily={BODY} fontSize={8.5} fontWeight={700} fill={C.bg} textAnchor="middle">
        Run what-if
      </text>
      <text x={276} y={126} fontFamily={MONO} fontSize={7} fill={C.faint}>
        RESULT
      </text>
      <text x={276} y={140} fontFamily={BODY} fontSize={8} fill={C.green}>
        pick succeeds (0.91)
      </text>
      <text x={276} y={152} fontFamily={BODY} fontSize={8} fill={C.muted}>
        vs 0.74 original
      </text>

      {/* predictions */}
      <Panel x={264} y={172} w={140} h={90} label="Predictions" />
      {predictions.map((p, i) => (
        <g key={p.l}>
          <Dot cx={276} cy={196 + i * 20} r={2.4} color={p.c} />
          <text x={284} y={199 + i * 20} fontFamily={BODY} fontSize={8} fill={C.muted}>
            {p.l}
          </text>
          <text x={394} y={199 + i * 20} fontFamily={MONO} fontSize={7.5} fill={p.c} textAnchor="end">
            {p.v}
          </text>
        </g>
      ))}

      {/* telemetry match */}
      <Panel x={414} y={12} w={132} h={250} label="Sync" accent={a} />
      <Ring cx={480} cy={66} r={30} frac={0.97} color={C.green} width={8} label="97%" sub="MATCH" />
      <text x={426} y={120} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        sim vs real
      </text>
      <text x={534} y={120} fontFamily={MONO} fontSize={7.5} fill={C.green} textAnchor="end">
        0.97
      </text>
      <Bar x={426} y={126} w={108} frac={0.97} color={C.green} h={4} />
      <text x={426} y={150} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        drift
      </text>
      <text x={534} y={150} fontFamily={MONO} fontSize={7.5} fill={C.text} textAnchor="end">
        3 cm
      </text>
      <Bar x={426} y={156} w={108} frac={0.03} color={C.green} h={4} />
      <line x1={426} y1={178} x2={534} y2={178} stroke={C.grid} />
      <text x={426} y={198} fontFamily={MONO} fontSize={7} fill={C.faint}>
        FRAMES RECORDED
      </text>
      <text x={534} y={198} fontFamily={DISPLAY} fontSize={13} fontWeight={700} fill={C.text} textAnchor="end">
        86.4k
      </text>
      <text x={426} y={220} fontFamily={MONO} fontSize={7} fill={C.faint}>
        REPLAY STORAGE
      </text>
      <text x={534} y={220} fontFamily={MONO} fontSize={8} fill={a} textAnchor="end">
        2.1 GB
      </text>
      <StatusPill x={426} y={238} label="live · synced" color={C.green} />
    </DashboardFrame>
  );
}

// ── OhhO Care ────────────────────────────────────────────────────────────────
function CareDash({ accent }: DashProps) {
  const a = accentHex(accent);
  const workOrders = [
    { id: "amr-17", part: "left knee motor", urg: "HIGH", c: C.red, days: 3 },
    { id: "amr-05", part: "wrist servo #3", urg: "MED", c: C.amber, days: 12 },
    { id: "amr-22", part: "battery pack", urg: "LOW", c: C.green, days: 30 },
  ];
  const motorTemp = [0.5, 0.52, 0.55, 0.58, 0.62, 0.68, 0.72, 0.78, 0.82, 0.85, 0.88, 0.91];
  return (
    <DashboardFrame title="ohho-care · fleet-maintenance" accent={accent} tools={["48 robots", "predictive"]}>
      {/* work orders */}
      <Panel x={14} y={12} w={190} h={250} label="Work orders" accent={a} />
      {workOrders.map((w, i) => (
        <g key={w.id}>
          <rect x={22} y={40 + i * 64} width={174} height={54} rx={7} fill="rgba(255,255,255,0.02)" stroke={w.c} strokeOpacity={0.4} />
          <text x={30} y={56 + i * 64} fontFamily={MONO} fontSize={9} fill={C.text} fontWeight={600}>
            {w.id}
          </text>
          <rect x={120} y={46 + i * 64} width={36} height={14} rx={4} fill={w.c} fillOpacity={0.18} stroke={w.c} strokeOpacity={0.5} />
          <text x={138} y={56 + i * 64} fontFamily={MONO} fontSize={7} fill={w.c} textAnchor="middle">
            {w.urg}
          </text>
          <text x={30} y={72 + i * 64} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
            {w.part}
          </text>
          <text x={30} y={86 + i * 64} fontFamily={MONO} fontSize={7.5} fill={w.c}>
            est. failure: {w.days}d
          </text>
        </g>
      ))}
      <rect x={22} y={236} width={174} height={20} rx={5} fill={a} />
      <text x={109} y={250} fontFamily={BODY} fontSize={8.5} fontWeight={700} fill={C.bg} textAnchor="middle">
        Order part from BOM
      </text>

      {/* motor degradation chart */}
      <Panel x={214} y={12} w={226} h={150} label="Motor temp · amr-17 left knee" accent={a} />
      <text x={426} y={30} fontFamily={DISPLAY} fontSize={14} fontWeight={700} fill={C.red} textAnchor="end">
        78°C
      </text>
      <text x={426} y={42} fontFamily={MONO} fontSize={7} fill={C.faint} textAnchor="end">
        threshold: 85°C
      </text>
      {[0, 1, 2].map((i) => (
        <line key={i} x1={226} y1={70 + i * 26} x2={430} y2={70 + i * 26} stroke={C.grid} />
      ))}
      <line x1={226} y1={62} x2={430} y2={62} stroke={C.red} strokeOpacity={0.3} strokeDasharray="4 3" />
      <text x={226} y={60} fontFamily={MONO} fontSize={6.5} fill={C.red}>
        threshold
      </text>
      <path d={sparkPath(motorTemp, 226, 56, 204, 80, true)} fill={C.red} fillOpacity={0.08} stroke="none" />
      <path d={sparkPath(motorTemp, 226, 56, 204, 80)} fill="none" stroke={C.red} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
      <text x={226} y={156} fontFamily={MONO} fontSize={7} fill={C.faint}>
        −60 min
      </text>
      <text x={430} y={156} fontFamily={MONO} fontSize={7} fill={C.red} textAnchor="end">
        now · trending up
      </text>

      {/* metrics */}
      <Panel x={214} y={172} w={226} h={90} label="Fleet metrics" />
      <text x={226} y={200} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        MTBF
      </text>
      <text x={320} y={200} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.text} textAnchor="end">
        412h
      </text>
      <text x={334} y={200} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        MTTR
      </text>
      <text x={430} y={200} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.text} textAnchor="end">
        3.4h
      </text>
      <text x={226} y={222} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        downtime (mo)
      </text>
      <text x={320} y={222} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.amber} textAnchor="end">
        18.2h
      </text>
      <text x={334} y={222} fontFamily={BODY} fontSize={8.5} fill={C.muted}>
        open WOs
      </text>
      <text x={430} y={222} fontFamily={DISPLAY} fontSize={12} fontWeight={700} fill={C.red} textAnchor="end">
        3
      </text>
      <StatusPill x={226} y={240} label="1 predictive" color={C.red} />

      {/* repair log */}
      <Panel x={450} y={12} w={96} h={250} label="Repair log" />
      {[
        { r: "amr-03 · wheel", t: "2d ago", c: C.green },
        { r: "amr-11 · IMU", t: "5d ago", c: C.green },
        { r: "amr-07 · arm", t: "1w ago", c: C.green },
      ].map((l, i) => (
        <g key={l.r}>
          <Dot cx={462} cy={44 + i * 24} r={2.6} color={l.c} />
          <text x={470} y={47 + i * 24} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
            {l.r}
          </text>
          <text x={534} y={47 + i * 24} fontFamily={MONO} fontSize={6.5} fill={C.faint} textAnchor="end">
            {l.t}
          </text>
        </g>
      ))}
      <line x1={462} y1={120} x2={534} y2={120} stroke={C.grid} />
      <text x={462} y={140} fontFamily={MONO} fontSize={7} fill={C.faint}>
        → COMPLY AUDIT
      </text>
      <StatusPill x={462} y={150} label="logged" color={C.green} />
      <text x={462} y={182} fontFamily={MONO} fontSize={7} fill={C.faint}>
        PARTS (BOM)
      </text>
      <text x={462} y={198} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        knee motor
      </text>
      <text x={534} y={198} fontFamily={MONO} fontSize={7} fill={a} textAnchor="end">
        $89
      </text>
      <text x={462} y={214} fontFamily={BODY} fontSize={7.5} fill={C.muted}>
        lead time
      </text>
      <text x={534} y={214} fontFamily={MONO} fontSize={7} fill={C.text} textAnchor="end">
        2 days
      </text>
      <StatusPill x={462} y={232} label="in stock" color={C.green} />
    </DashboardFrame>
  );
}

const DASHBOARDS: Record<string, (p: DashProps) => JSX.Element> = {
  build: BuildDash,
  bench: BenchDash,
  frame: FrameDash,
  connect: ConnectDash,
  bridge: BridgeDash,
  serve: ServeDash,
  view: ViewDash,
  data: DataDash,
  train: TrainDash,
  autonomy: AutonomyDash,
  mind: MindDash,
  market: MarketDash,
  pilot: PilotDash,
  fleet: FleetDash,
  twin: TwinDash,
  care: CareDash,
  comply: ComplyDash,
  shield: ShieldDash,
  proof: ProofDash,
};

export default function ProductDashboard({ slug, accent }: { slug: string; accent: Accent }) {
  const Dash = DASHBOARDS[slug];
  if (!Dash) return null;
  return <Dash accent={accent} />;
}
