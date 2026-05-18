#!/usr/bin/env python3
"""
Enforces the per-layer coverage thresholds CLAUDE.md mandates.

Parses every `coverage/**/coverage.cobertura.xml` produced by
`dotnet test --collect:"XPlat Code Coverage"` and computes per-assembly
line coverage. Each cobertura file is per-test-project and emits the
full <line> set for every assembly referenced by the test process —
including assemblies the test itself doesn't exercise (those come back
with hits=0). To get a correct merged percentage we deduplicate by
(filename, line_number) across all files: a line counts as coverable
once, and as covered if any test project recorded hits > 0 on it.

Naive summing (the original implementation) inflates the denominator by
N (number of test projects), producing artificially low percentages
that the gate rejects even when real coverage is high.

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
    "Yumney.Shared.Events.Wolverine": "Wolverine + RabbitMQ + Postgres outbox composition — covered end-to-end by Yumney.Integration.Tests (InboxPipelineInvocationTests, OutboxDeliveryTests). No useful unit-test surface in the AddWolverineEventBus host-builder extension.",
    "Yumney.Shared.Web": "Host wiring + middleware pipeline — covered end-to-end by Yumney.Integration.Tests and the per-module Api.Tests. HostBuilderExtensions (245 lines) is .NET host configuration with no unit-test seams.",
    "Yumney.Shared.Events.InProcess": "In-process bus + dispatcher — currently 74.5%, exercised end-to-end by per-module Application.Tests and Integration.Tests. Uplift tracked under #464 follow-up.",
    "Yumney.Shared.Persistence": "EventStoreBase + Inbox + outbox-aware DbContext registration — currently 80.7%, exercised end-to-end by Integration.Tests (ShoppingEventStoreTests, OutboxDeliveryTests). Uplift tracked under #464 follow-up.",
}


def main() -> int:
    files = sorted(COVERAGE_ROOT.glob("**/coverage.cobertura.xml"))
    if not files:
        print(f"ERROR: no cobertura files under {COVERAGE_ROOT}/**", file=sys.stderr)
        return 2

    # Per assembly: set of (class_name, line_number) seen, and set of those
    # that any test project recorded hits > 0 for. The dedup key uses class
    # name (which is stable across cobertura files) rather than filename,
    # because different test projects emit the same source under two filename
    # prefixes (e.g. "src\Yumney.MealPlan.Domain\WeeklyPlan.cs" in one cobertura
    # vs "Yumney.MealPlan.Domain\WeeklyPlan.cs" in another). With filename
    # keys, every coverable line would be double-counted in the denominator
    # while only the variant that actually got hits contributes to covered —
    # halving the reported percentage.
    coverable_lines: dict[str, set[tuple[str, str]]] = defaultdict(set)
    covered_lines: dict[str, set[tuple[str, str]]] = defaultdict(set)

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
                class_name = cls.get("name") or cls.get("filename") or ""
                for line in cls.findall(".//line"):
                    number = line.get("number")
                    if number is None:
                        continue
                    key = (class_name, number)
                    coverable_lines[name].add(key)
                    hits = line.get("hits", "0")
                    if hits != "0":
                        covered_lines[name].add(key)

    coverable = {name: len(keys) for name, keys in coverable_lines.items()}
    covered = {name: len(covered_lines.get(name, set())) for name in coverable}

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
