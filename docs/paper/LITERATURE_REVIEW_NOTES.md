# Literature Review Evidence Notes

## Scope and evidence standard

These notes support the planned systems paper, **“OmniBot: An Affordable, Distributed Embodied-AI Platform for Mobile Manipulation with Vision-Language-Action Models.”** They distinguish what is directly established by cited work from the contribution that OmniBot must validate experimentally. No internal benchmark target or planned measurement is treated as an experimental result.

## Verified sources

| ID | Work | Directly supported finding | Relevance to OmniBot |
|---|---|---|---|
| L1 | Fu, Zhao, and Finn, *Mobile ALOHA* (2024) | Presents a low-cost, open mobile bimanual manipulation system, whole-body teleoperation, onboard laptop computing, and evaluation on long-horizon household tasks. Its described stack uses a differential-drive AgileX Tracer base, a consumer laptop with RTX 3070 Ti, and three RGB cameras. | Establishes that affordable/mobile whole-body data collection is a credible comparison theme, but it does not establish the value of OmniBot's specific Pi-to-GPU split, ROS 2 SLO benchmarking, or single-arm holonomic platform. Avoid unsupported claims that it lacks all distributed computing or system benchmarks; instead compare the published evaluation emphasis and configuration precisely. |
| L2 | Kim et al., *OpenVLA* (2025) | Introduces an open 7B VLA trained on 970k real-world demonstrations from Open X-Embodiment; evaluates generalist manipulation, fine-tuning, and quantized serving on consumer-grade GPUs. | Strong foundation-model reference and a necessary dependency citation. It supplies a model-level deployment/fine-tuning contribution, not validation of an end-to-end low-cost mobile-manipulation deployment across Pi, ROS 2, a networked GPU server, a safety mux, and reproducible system-wide latency metrics. |

## Early comparison conclusion

The defensible novelty claim is **not** “the first affordable mobile manipulator” nor “the first open VLA deployment.” The paper should instead test whether a reproducible, low-cost **system integration** of a Raspberry Pi control/perception edge node, networked GPU policy service, ROS 2 coordination, hybrid autonomy arbitration, and SLO-aware benchmarks yields an auditable deployment pathway. This needs comparisons against a clearly documented local/on-robot policy configuration, not qualitative assertions.

## Source URLs

- L1: https://arxiv.org/abs/2401.02117
- L2: https://arxiv.org/abs/2406.09246

| L3 | Alshiekh et al., *Safe Reinforcement Learning via Shielding* (2018) | Defines a shield as a reactive monitor that corrects learner actions that would violate a specified safety property. The contribution is a formal safety mechanism, not merely source selection or velocity clamping. | Frames the terminology correctly. OmniBot must call its current component a **safety-bounded action multiplexer** unless it supplies formal specifications, monitoring semantics, and safety guarantees sufficient to justify “shield.” It can cite shielding as future-work context. |
| L4 | Open X-Embodiment Collaboration, *Open X-Embodiment* (2024) | Releases a large open real-robot dataset and RT-X models that support cross-embodiment robot learning. | Establishes the data/model ecosystem on which OpenVLA and Octo build, but not low-cost system deployment. OmniBot should cite it for upstream training data and avoid implying that it validates its own task success or latency. |

## Refined technical claim boundary

The architecture section should distinguish three control-plane mechanisms: **(1)** a deterministic command source selector / mux, **(2)** hard, documented actuator limits and an emergency stop, and **(3)** an optional future **formal safety shield**. Conflating these levels would be technically incorrect and vulnerable in review.

## Additional source URLs

- L3: https://doi.org/10.1609/aaai.v32i1.11797
- L4: https://arxiv.org/abs/2310.08864

| L5 | Shukor et al., *SmolVLA* (2025) | Introduces a compact VLA intended for training on a single GPU and deployment on consumer GPUs or CPUs; proposes asynchronous inference that decouples perception/action prediction from execution. | Directly relevant model-level baseline. It weakens any claim that consumer-viable VLA deployment is itself novel. OmniBot’s novelty must be at the **embodied platform and reproducible distributed systems evaluation** level, including Pi control/perception, network service behavior, and end-to-end task/control metrics. |
| L6 | Zitkovich et al., *RT-2* (2023) | Establishes the VLA paradigm by representing robot actions as tokens and co-fine-tuning vision-language models with robot trajectories and web-scale vision-language tasks; evaluates generalization in 6,000 robot trials. | Foundational context for VLA-enabled behaviour. It is not an appropriate direct hardware-cost or latency baseline because its scientific question and hardware setting differ from OmniBot’s systems question. |

## Implication for the related-work section

The section should be organized by **research question**, not by project names. Foundation-model papers (RT-1, RT-2, OpenVLA, Octo, SmolVLA) answer questions about policy scale, pretraining, adaptation, and policy inference. OmniBot should cite them as enabling components while stating that it evaluates an operational stack rather than proposing a new policy architecture or claiming competitive policy success against their controlled benchmarks.

## Additional source URLs

- L5: https://arxiv.org/abs/2506.01844
- L6: https://proceedings.mlr.press/v229/zitkovich23a.html

| L7 | Mayoral-Vilches et al., *RobotPerf* (2024) | Provides an open, ROS 2-based reference benchmark suite for fair evaluation of robotics-computing performance and hardware/software trade-offs. | Direct antecedent for reproducible ROS 2 performance evaluation. OmniBot must not claim to introduce generic robotics benchmarking. Its potential contribution is a **platform-specific SLO-gated protocol** spanning motor protocol, kinematics, perception, policy serving, communication, and task-level execution—preferably cross-referenced to RobotPerf principles where the metrics overlap. |
| L8 | Open Navigation, *Nav2 Documentation* (accessed 2026) | Nav2 provides modular perception, planning, control, localization, and behaviour-tree orchestration for holonomic and non-holonomic mobile bases, producing velocity commands when properly configured. | Establishes the classical navigation baseline. OmniBot’s contribution cannot be Nav2 itself; it is the integration boundary between Nav2 commands and learned policy/VLA commands, plus the reproducible measurement of that boundary. |

## Refined systems gap

Existing ROS 2 and robotics-computing frameworks provide the building blocks for navigation and component benchmarking. The paper’s gap should therefore be stated narrowly: there is limited **open, system-level evidence** showing how an affordable mobile manipulator composes these components with networked VLA serving, bounded command arbitration, and a repeatable measurement protocol. The paper should test this composition rather than assert it is unprecedented.

## Additional source URLs

- L7: https://accelerationrobotics.com/robotperf.php
- L8: https://docs.nav2.org/

| L9 | Khazatsky et al., *DROID* (2024) | Introduces a large in-the-wild robot-manipulation dataset and a distributed data-collection setup; reported scale is 76k demonstrations. | Relevant to the data-collection context and eventual data-paper follow-on, not to the first systems paper’s main evaluation. It shows the field’s emphasis on data diversity and transfer but does not substitute for a deployment-latency study. |
| L10 | Walke et al., *BridgeData V2* (2023) | Releases 53,896 trajectories across 24 environments using a publicly available low-cost robot, with evaluations of scalable imitation/offline-RL methods. | Shows that low-cost hardware and open data resources are established. OmniBot should not claim novelty from low cost alone; its differentiator must remain mobile manipulation plus distributed deployment and end-to-end, reproducible systems measurements. |

## Claim discipline checkpoint

The literature rules out three overly broad novelty claims: **“first low-cost robot,” “first low-cost robot-learning dataset,” and “first consumer-hardware VLA.”** The draft must use a conditional evidence claim: *we provide an open reference implementation and measurement study for this specific hardware-and-software composition.* That claim becomes valid only when the paper releases the bill of materials, pinned software configuration, raw benchmark artefacts, and protocol.

## Additional source URLs

- L9: https://arxiv.org/abs/2403.12945
- L10: https://proceedings.mlr.press/v229/walke23a.html

| L11 | Cui et al., *AhaRobot* (2025) | Describes an open-source bimanual mobile manipulator built from off-the-shelf parts, with a reported $1,000 hardware budget without compute and $2,000 with a Mini-ITX + RTX 4060 computer; its core contributions are hardware, precision-control compensation, and teleoperation. | A decisive contemporary comparator. It confirms the low-cost mobile-manipulation design space is active. OmniBot should compare configuration and price assumptions transparently, but must not assert a categorical affordability first. Its distinct study should centre on a separate Pi edge node plus GPU workstation, a single-arm holonomic base, ROS 2 composition, and reproducible full-pipeline metrics. |

## Affordability comparison rule

All cost comparisons must use the same accounting boundary. Report at least **(a)** robot-only hardware, **(b)** robot plus on-robot compute, and **(c)** total experimental system cost including the remote GPU workstation, networking, cameras, battery, and peripherals. A headline “$500” figure cannot be compared directly to systems that include different compute or sensor assumptions.

## Additional source URL

- L11: https://arxiv.org/abs/2503.10070

| L12 | Wu et al., *TidyBot++* (2025) | Presents an open-source holonomic mobile manipulator for robot learning, using powered casters and a mobile-phone teleoperation interface. The paper reports a $5–6k low-cost base and task-policy evaluations. | A direct architectural comparator because both platforms are holonomic mobile manipulators for policy learning. It makes holonomy and open-source hardware non-novel by themselves. OmniBot must differentiate through the lower-cost mecanum configuration and prove—not assume—the benefit of its edge/GPU deployment and systems measurement methodology. |

## Holonomy implication

The manuscript should explain that OmniBot uses a **mecanum omnidirectional base**, while TidyBot++ uses **powered casters** claimed to be fully holonomic. Do not call all omnidirectional bases “holonomic” without a kinematic definition. In the OmniBot manuscript, describe the commanded planar degrees of freedom and the relevant wheel-kinematics model precisely, then treat any data-collection usability claim as a hypothesis requiring task data rather than a general truth.

## Additional source URL

- L12: https://arxiv.org/abs/2412.10447
