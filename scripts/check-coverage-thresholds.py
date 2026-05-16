#!/usr/bin/env python3
"""
Enforces the per-layer coverage thresholds CLAUDE.md mandates.

Parses every `coverage/**/coverage.cobertura.xml` produced by
`dotnet test --collect:"XPlat Code Coverage"` and computes per-assembly
line coverage. Each cobertura file is per-test-project; multiple test
projects can cover the same production assembly, so coverable + covered
lines are summed across files before computing the percentage.

Exit codes:
  0 — every blocking threshold met (warnings may still be printed)
  1 — at least one blocking threshold missed
  2 — no coverage files found

CLAUDE.md table:
  Domain          90%   blocking
  Application     80%   blocking
  Shared.*        90%   blocking
  Infrastructure  50%   warn
  Components      70%   warn  (frontend, enforced via Vitest configs, not here)
"""

import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

COVERAGE_ROOT = Path("coverage")

# (substring matched against assembly name, threshold percent, blocking)
THRESHOLDS = [
    (".Domain", 90, True),
    (".Application", 80, True),
    (".Shared.", 90, True),
    (".Infrastructure", 50, False),
]

# Assemblies that don't yet meet their threshold but are tracked for uplift.
# Every entry is technical debt — add a ticket reference and a target date.
# Remove the entry once the assembly passes the gate.
EXEMPTIONS: dict[str, str] = {
    # name -> ticket / reason
    "Yumney.Shared.Guards": "Coverage uplift tracked under #464 follow-up — currently ~83%, target 90%.",
}


def main() -> int:
    files = sorted(COVERAGE_ROOT.glob("**/coverage.cobertura.xml"))
    if not files:
        print(f"ERROR: no cobertura files under {COVERAGE_ROOT}/**", file=sys.stderr)
        return 2

    # Sum coverable + covered lines per assembly across every test project's XML.
    coverable: dict[str, int] = defaultdict(int)
    covered: dict[str, int] = defaultdict(int)

    for path in files:
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError as ex:
            print(f"WARN skipping malformed {path}: {ex}", file=sys.stderr)
            continue
        for package in root.findall(".//package"):
            name = package.get("name")
            if not name:
                continue
            for cls in package.findall(".//class"):
                for line in cls.findall(".//line"):
                    coverable[name] += 1
                    hits = line.get("hits", "0")
                    if hits != "0":
                        covered[name] += 1

    failures: list[str] = []
    warnings: list[str] = []
    exempted: list[str] = []

    for name in sorted(coverable):
        total = coverable[name]
        if total == 0:
            continue
        pct = covered[name] / total * 100
        for pattern, threshold, blocking in THRESHOLDS:
            if pattern in name:
                if pct < threshold:
                    line = f"  {name}: {pct:.1f}% < {threshold}% ({covered[name]}/{total} lines)"
                    if name in EXEMPTIONS:
                        exempted.append(f"{line}  [EXEMPT: {EXEMPTIONS[name]}]")
                    elif blocking:
                        failures.append(line)
                    else:
                        warnings.append(line)
                break

    if exempted:
        print("EXEMPT coverage shortfalls allow-listed for uplift:")
        for line in exempted:
            print(line)

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
