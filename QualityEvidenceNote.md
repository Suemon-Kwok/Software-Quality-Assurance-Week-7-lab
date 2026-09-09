# Quality evidence note

## Activity 1 — Measurement context

**Q20. Which values are raw observations or measures?**
Every column in `booking-events.csv` (timestamp, release, requestId, isValidRequest, outcome, timeBand, channel, latencyMs) is a raw observation — something recorded directly from what actually happened, with no calculation or judgement applied. The historical figures in `release-history.csv` (validSuccessRatePercent, p50/p95LatencyMs, peakHourSuccessRatePercent for past releases) are also observations, just already aggregated per release rather than per request.

**Q21. Which values are targets rather than observations?**
The three values in `quality-targets.json` — `minimumValidSuccessRatePercent`, `maximumP95LatencyMs`, `minimumPeakHourSuccessRatePercent`. These are not things that happened; they are thresholds someone decided the system *should* meet.

**Q22. Is a request rejected because mandatory input is missing a system failure?**
No. A system failure means the system did not do what it was supposed to do. Correctly rejecting a request with missing or invalid mandatory input is the validation logic working as designed — it is intended, correct behaviour, not a failure. Counting these rejections as failures would penalise the system for behaving correctly and could push people to weaken validation just to improve the number.

**Q23. Why is the phrase "booking success rate" incomplete?**
Because it does not state the denominator. Success out of what population — all requests including invalid ones, or valid requests only? Over which release, and which time window? Without an explicit denominator and scope, two people could calculate very different numbers and both legitimately call the result "booking success rate."

**Q24. What important context would be lost if all releases and time bands were combined?**
Visibility into where risk is concentrated. Combining Normal and Peak hours would hide that peak-hour latency and failures are markedly worse (peak latencies reach 1800ms vs. ~800ms in normal hours, and both `SystemFailure` observations occur during Peak). Combining releases would hide the release-over-release trend visible in `release-history.csv` — valid success rate declining from 95% to 90% and P95 latency rising from 950ms to 1450ms across releases.

## Current observations

For release 2.3.0 (24 total observations: 20 valid, 4 rejected-invalid):

- **Valid-request success rate:** 18/20 = **90.0%** (target: ≥90% — meets target exactly, no margin)
- **P50 latency:** **700 ms**
- **P95 latency:** **1450 ms** (target: ≤1500 ms — meets target, 50 ms margin)
- **Normal-hour success rate:** 10/10 = **100%**
- **Peak-hour success rate:** 8/10 = **80%** (target: ≥80% — meets target exactly, no margin)

All three targets are technically met, but two of the three (valid success rate, peak-hour success rate) are sitting exactly on the threshold with zero margin.

## Comparisons and interpretation

- **What does the release trend reveal that the latest snapshot does not?** Across releases 2.0.0 → 2.3.0, valid success rate has declined every release (95% → 93% → 92% → 90%), P95 latency has risen every release (950 → 1100 → 1250 → 1450 ms), and peak-hour success has declined faster than the overall rate (90% → 86% → 84% → 80%). The snapshot alone shows "targets met," which looks fine in isolation. The trend shows these are not comfortable results but the last point on a consistent four-release slide — if the trajectory continues unchanged, the next release is likely to breach both the valid-success and peak-hour targets.

- **What does segmentation reveal that the aggregate result conceals?** The aggregate 90% valid success rate hides that Normal hours are performing perfectly (100%) while all of the degradation is concentrated in Peak hours (80%). Both `SystemFailure` observations occur in Peak hours specifically — and both also happen to be on the Mobile channel (R018, R020), while Peak-hour Web requests were 100% successful (5/5). The aggregate number treats the system as uniformly "mostly fine," when the real picture is "fine except for a specific peak+mobile combination."

- **Why could average latency appear acceptable while some users still experience long waits?** The mean latency across all 20 valid requests is about 833 ms — comfortably under the 1500 ms target — because it is pulled down by the ten fast Normal-hour requests (350–800 ms). But the slowest Peak-hour requests reach up to 1800 ms, above the target. An average blends the fast majority with the slow minority into one number that represents neither group well. This is exactly why the lab specifies P95 rather than an average: P95 is defined to expose the slow tail instead of smoothing over it.

- **How does the qualitative feedback complement the numerical evidence?** The feedback lines up with the numbers in specific, useful ways: F04 and F06 describe a Mobile, peak-hour failure with a confusing retry/error experience — matching the two `SystemFailure` rows, both Mobile and Peak. F03 and F05 describe peak-hour slowness that still completes — consistent with Peak latencies running up to 1800 ms while still mostly succeeding. F01, F02 and F08 describe smooth Normal-hour experiences — consistent with 100% Normal-hour success. F07 describes a validation message correctly catching a missing field — supporting the Activity 1 conclusion that rejecting invalid input is correct behaviour, not a failure. The numbers say *what* is happening (where failures and latency cluster); the feedback suggests *what it feels like* and hints at a possible channel-specific angle (Mobile) worth investigating that the current metric definitions don't segment by.

## Decision-oriented indicators

1. **Valid-request success rate, with trend** — the core reliability signal; the four-release decline is more decision-relevant than any single value.
2. **Peak-hour success rate** — isolates where risk is concentrated, since Normal-hour performance is not the concern.
3. **P95 latency, with P50 shown alongside it** — P50 gives the typical experience, P95 gives the tail; together they show whether a widening gap between them is developing.
4. **Peak-hour failure count by channel (Mobile vs Web)** — not one of the three formally defined metrics, but the data and feedback both point to a Mobile-specific concentration worth tracking explicitly rather than leaving buried inside the aggregate peak-hour figure.

## Action, inference and unknown

- **Evidence-supported action:** Prioritise an investigation into peak-hour, Mobile-channel booking failures before the next release ships — both recorded system failures, and the two most concerning pieces of user feedback, share exactly that combination.
- **Reasonable inference:** The failures may be related to how the system behaves under load specifically on the Mobile channel during peak hours (e.g. timeout handling, retry behaviour), given the pattern in both the quantitative and qualitative evidence — but this is an inference, not something the current data proves.
- **Important unknown:** The dataset does not contain the actual cause of the `SystemFailure` outcome (e.g. timeout, downstream dependency, code defect, network condition). Root cause requires evidence this metric set was not designed to capture, and the feedback sample (8 comments) is far too small to generalise from.

## Limitations

The dataset is synthetic and small (24 observations for the current release), so percentiles and segment rates are based on very few data points — a single additional failure or success would shift the peak-hour rate by 10 percentage points. The metric definitions treat "peak" and "normal" as pre-labelled categories without defining the underlying time boundaries. The peak-hour failure-rate baseline in Activity 2 was derived indirectly (as the complement of `peakHourSuccessRatePercent`), which assumes only two possible outcomes for valid peak requests — an assumption that could break if new outcome types are introduced. The user feedback is a constructed sample of 8 comments, explicitly not representative user research, and should be read as illustrative context rather than statistical evidence. None of these metrics identify a root cause; they indicate where to look, not why it is happening.

## Vanity metric

**Average (mean) latency** is the vanity metric here. It looks attractive on a dashboard — a single, always-present number that in this release (≈833 ms) sits comfortably under target and would make the release look healthy. But as shown above, it actively conceals the problem: it is dominated by the many fast Normal-hour requests and does not surface the 1800 ms worst case or the Peak/Mobile failure cluster at all. A metric that gets *better-looking* precisely when you ignore the segment where the real problem lives is a weak, potentially misleading one — P95 combined with segmentation is a stronger substitute for the same underlying question.

