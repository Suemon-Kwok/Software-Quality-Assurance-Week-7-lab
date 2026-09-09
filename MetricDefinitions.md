# Metric definitions

Complete one contract for each required metric. Begin with the quality goal and question, then define the calculation.

## Metric 1 — Valid booking success rate

- **Quality goal:** Ensure that valid appointment requests reliably complete successfully.
- **Quality question:** What proportion of valid booking requests completed successfully?
- **Metric name and formula:** Valid-request success rate (%) = (successful valid requests ÷ all valid requests) × 100
- **Numerator:** Valid requests with `outcome = Success`
- **Denominator:** All requests where `isValidRequest = true`
- **Included observations:** All valid requests for the current release, regardless of channel or time band
- **Excluded observations:** Requests where `isValidRequest = false` (rejected for missing/invalid mandatory input). These are correct system behaviour, not failures (see Activity 1, Q22), and answer a different question than reliability of valid requests
- **Release, environment and time scope:** Current release only (2.3.0, the most recent release in `booking-events.csv`), using the supplied synthetic dataset
- **Segment or breakdown:** Normal hours vs. Peak hours
- **Baseline:** Immediately prior release, 2.2.0, at 92%. Read alongside the broader trend across releases (95% → 93% → 92% → 90%), which is declining
- **Target or warning threshold:** ≥ 90% (`minimumValidSuccessRatePercent` in `quality-targets.json`)
- **Decision or action supported:** If the value falls below target, or the declining trend continues, investigate the cause of valid-request failures and decide whether the release should proceed
- **Limitation or possible misuse:** The rate can be inflated by tightening validation rules to reject more requests as invalid, shrinking the denominator without improving actual reliability. It also does not identify *why* valid requests fail.

## Metric 2 — P95 booking latency

- **Quality goal:** Ensure booking response times remain acceptable even for the slower end of user experiences, not just on average.
- **Quality question:** At or below what response time do 95% of valid booking requests complete?
- **Metric name and formula:** P95 latency (ms) = the latency value at rank `ceiling(0.95 × n)` when valid-request latencies are sorted ascending (nearest-rank method)
- **Population and unit:** Latency in milliseconds, measured over valid requests only
- **Percentile convention:** Nearest-rank method: sort observations ascending, calculate `ceiling(percentile × count)`, and select that one-based rank. Stated explicitly because different tools use different interpolation conventions
- **Included observations:** `latencyMs` values from valid requests (`isValidRequest = true`) in the current release
- **Excluded observations:** Invalid/rejected requests — their latency reflects how quickly a request was rejected, not how long a booking took to complete
- **Release, environment and time scope:** Current release only (2.3.0), synthetic dataset
- **Segment or breakdown:** Compare P50 against P95 to detect a long tail of slow experiences; compare Normal-hour vs. Peak-hour latency
- **Baseline:** Immediately prior release, 2.2.0, at 1250 ms. The broader trend (950 → 1100 → 1250 → 1450 ms) is rising release over release
- **Target or warning threshold:** ≤ 1500 ms (`maximumP95LatencyMs` in `quality-targets.json`)
- **Decision or action supported:** If P95 approaches or exceeds target, or the rising trend continues, prioritise a performance/capacity investigation, particularly during peak hours, before the next release
- **Limitation or possible misuse:** A single P95 figure does not explain *why* the tail is slow, and can look acceptable in aggregate while a specific segment (e.g. peak hours) is experiencing much worse performance. The nearest-rank calculation is also coarser with small sample sizes.

## Metric 3 — Peak-hour system-failure rate

- **Quality goal:** Ensure the system remains reliable during high-demand (peak) periods, when failure risk is highest.
- **Quality question:** What proportion of valid peak-hour requests result in a system failure?
- **Metric name and formula:** Peak-hour system-failure rate (%) = (valid peak-hour requests with `outcome = SystemFailure` ÷ all valid peak-hour requests) × 100
- **Numerator:** Valid requests where `timeBand = Peak` and `outcome = SystemFailure`
- **Denominator:** All valid requests where `timeBand = Peak`
- **Meaning of "peak hour" and "system failure":** "Peak hour" is the `timeBand = Peak` value as recorded in the dataset. "System failure" is the `SystemFailure` outcome — the system failing to process a request that was itself valid — distinct from `RejectedInvalid`, which is correct behaviour (see Activity 1, Q22)
- **Included observations:** Valid requests with `timeBand = Peak` in the current release
- **Excluded observations:** Invalid/rejected requests, and all Normal-hour requests
- **Release, environment and time scope:** Current release only (2.3.0), synthetic dataset
- **Segment or breakdown (revised — see audit below):** Channel (Mobile vs. Web) **within** Peak hours, in addition to the Peak-vs-Normal split. Both recorded system failures were Peak **and** Mobile, while Peak+Web was 100% successful — the time-band split alone conceals this concentration
- **Baseline:** Derived from `release-history.csv`: 2.2.0 had 84% peak-hour success, implying a 16% peak-hour failure rate (assuming valid peak requests resolve to either Success or SystemFailure). The trend (10% → 14% → 16% → 20%, i.e. 100% minus the recorded peak success rate) is worsening
- **Target or warning threshold:** No explicit failure-rate target is supplied, but `minimumPeakHourSuccessRatePercent` (80%) implies a maximum acceptable peak-hour failure rate of 20%
- **Decision or action supported:** If the failure rate exceeds the implied 20% threshold, or the worsening trend continues, prioritise a peak-hour reliability and capacity investigation ahead of the next release — specifically checking Mobile-channel behaviour under peak load first, given the current evidence
- **Limitation or possible misuse:** This metric assumes valid peak requests only ever resolve to Success or SystemFailure; if other outcome categories are introduced later, the derived baseline/threshold relationship breaks. It also does not identify the cause of failures (e.g. timeout, capacity, downstream dependency), and could be gamed by reclassifying failures under a different outcome label.

## Activity 5 — Metric audit (Metric 3: peak-hour system-failure rate)

- **46. What exactly is measured?** The percentage of valid Peak-hour requests in the current release resulting in `SystemFailure`.
- **47. Scope, denominator, data source, assumptions:** Current release only, sourced from `booking-events.csv`; denominator is valid Peak-hour requests; assumes the `TimeBand` label is reliable and that valid Peak requests resolve only to `Success` or `SystemFailure`.
- **48. Baseline/target:** Baseline 16% (derived from 2.2.0); implied target/threshold of 20% failure (from the 80% minimum success target).
- **49. Decision/action:** Prioritise a peak-hour reliability investigation, and treat as a possible release-gating factor if it worsens.
- **50. Missing/invalid/duplicated data risk:** Only 10 valid Peak observations — one event shifts the rate by 10 points; a duplicated row would double-count a failure; an unexpected outcome value would be silently excluded.
- **51. Could it be gamed?** Yes — by reclassifying failures under a different outcome label, or by shifting traffic so fewer requests are timestamped as "Peak."
- **52. What remains outside this metric?** Failure-communication quality (per user feedback), and — critically — the fact that the risk is concentrated in the Mobile channel specifically, not Peak hours broadly.
- **53. How should it be communicated?** Segmented and trended, not as a bare snapshot or threshold pass/fail — the segment is where the real signal is.

**Concrete change made:** Added **Channel (Mobile vs. Web)** as a required segment for this metric, alongside the existing Peak-vs-Normal split.
**Why this makes the metric less ambiguous / less likely to be misused:** Without it, a team could see "peak-hour failure rate improved" after a change that only helped Web traffic, while the actual Mobile-specific problem — the one both data points and user feedback agree is real — remains completely hidden. The revised contract forces whoever reports this metric to check the channel breakdown before declaring the peak-hour risk resolved.

