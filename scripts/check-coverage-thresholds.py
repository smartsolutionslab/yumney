#!/usr/bin/env python3
"""
Enforces the per-layer coverage thresholds CLAUDE.md mandates.

Run AFTER `dotnet-reportgenerator-globaltool` has emitted a JsonSummary into
`coverage-report/Summary.json` from the merged cobertura inputs.

Exit codes:
  0 — every blocking threshold met (warnings may still be printed)
  1 — at least one blocking threshold missed
  2 — Summary.json missing or malformed

CLAUDE.md table:
  Domain          90%   blocking
  Application     80%   blocking
  Shared.*        90%   blocking
  Infrastructure  50%   warn
  Components      70%   warn  (handled by the frontend Vitest configs, not here)
"""

import json
import sys
from pathlib import Path

SUMMARY_PATH = Path("coverage-report/Summary.json")

# (substring matched against assembly name, threshold percent, blocking)
THRESHOLDS = [
    (".Domain", 90, True),
    (".Application", 80, True),
    (".Shared.", 90, True),
    (".Infrastructure", 50, False),
]


def main() -> int:
    if not SUMMARY_PATH.exists():
        print(f"ERROR: {SUMMARY_PATH} not found — did ReportGenerator run?", file=sys.stderr)
        return 2

    data = json.loads(SUMMARY_PATH.read_text(encoding="utf-8"))
    assemblies = data.get("summary", {}).get("assemblies") or data.get("assemblies") or []
    if not assemblies:
        # ReportGenerator sometimes nests under "summary" and sometimes flat; tolerate both
        # but report when neither holds anything.
        print("ERROR: no assemblies found in coverage summary", file=sys.stderr)
        return 2

    failures: list[str] = []
    warnings: list[str] = []
    skipped: list[str] = []

    for assembly in assemblies:
        name = assembly.get("name") or "<unknown>"
        coverable = assembly.get("coverableLines") or assembly.get("CoverableLines") or 0
        covered = assembly.get("coveredLines") or assembly.get("CoveredLines") or 0
        if coverable == 0:
            skipped.append(name)
            continue
        pct = covered / coverable * 100
        matched = False
        for pattern, threshold, blocking in THRESHOLDS:
            if pattern in name:
                matched = True
                if pct < threshold:
                    line = f"  {name}: {pct:.1f}% < {threshold}%"
                    (failures if blocking else warnings).append(line)
                break
        if not matched:
            # Outside the gated tiers (e.g. Api hosts, Tests, AppHost) — not a gate.
            pass

    if warnings:
        print("WARN coverage warnings (non-blocking, see CLAUDE.md):")
        for line in warnings:
            print(line)

    if failures:
        print("FAIL coverage thresholds below CLAUDE.md gates:")
        for line in failures:
            print(line)
        return 1

    print("OK every blocking coverage threshold met")
    return 0


if __name__ == "__main__":
    sys.exit(main())
