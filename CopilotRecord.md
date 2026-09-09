# Copilot usage record

Fill this in from your own Copilot session while working on Activity 3 (or any part of this lab). One real prompt, one real suggestion, one real correction or rejection.

**Prompt used:**
(e.g. "Help me implement ValidSuccessRateByTimeBand in this C# project. Use only the fields shown below. Explain the denominator and empty-data behaviour before providing code.")

**Useful suggestion Copilot gave:**
(What did it get right or save you time on?)

**Correction or rejection, and why:**
Accept — the denominator explanation:

Copilot's answer that the denominator should be "valid attempts inside the time band" and shouldn't include null/unknown/ignored samples matches exactly what the lab requires and what's already implemented in ValidSuccessRateByTimeBand. This is useful because it independently confirms your understanding of the metric — good to note as your "useful suggestion" in the record.



Reject #1 — the file-search hallucination attempt:

Look at the task panel: Copilot tried to search for src/Domain/Telemetry/Models/SuccessRateByTimeBand.cs — a file and folder structure that doesn't exist anywhere in this project (this project doesn't even have a src/ folder, let alone a Domain/Telemetry namespace). It failed with a red X, which is good — but it shows Copilot guessing at a plausible-sounding enterprise-style path instead of checking the actual project structure. This is a textbook example of the lab's warning: "Reject invented filenames, commands, data columns, APIs or assumptions about the dataset." Worth quoting directly in your record as the correction.



Reject #2 — the nullable-return suggestion:

This one's subtler and arguably the more interesting one to write up. Copilot recommends returning a nullable double? (or null) when a band has zero valid attempts, instead of 0%. That's a reasonable idea in isolation — but it doesn't fit how the actual method is built. Look at the real implementation: it filters to valid requests before grouping (.Where(IsValidRequest).GroupBy(TimeBand)). That means a time band with zero valid attempts never produces a group at all — it's simply absent from the dictionary, not present with a null value. So:



Copilot's fix assumes a Dictionary<string, double?> (or similar) return type

The lab's actual method signature returns IReadOnlyDictionary<string, double> (non-nullable) — Copilot's suggestion wouldn't even compile against the given signature without further rework

Both approaches solve the "0% is misleading" problem, but in incompatible ways — you'd have to pick one contract and stick with it, not paste Copilot's answer in blindly

