import { type Accent } from "./dashboardKit";

export default function ProductDashboard({ slug, accent }: { slug: string; accent: Accent }) {
  // Use the generated poster image as a fallback for the generated video
  return (
    <div className="relative w-full aspect-video rounded-[14px] overflow-hidden" style={{ background: "#0A0E1A" }}>
      <video
        autoPlay
        muted
        loop
        playsInline
        poster={`/videos/products/${slug}.png`}
        className="w-full h-full object-cover"
      >
        <source src={`/videos/products/${slug}.mp4`} type="video/mp4" />
      </video>
    </div>
  );
}
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
