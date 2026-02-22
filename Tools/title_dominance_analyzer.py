from __future__ import annotations

"""Title dominance analysis and simulation tool.

This script parses title definitions from Tracking/TitlesManager.cs and analyzes
requirement-set dominance relations.

Core ideas:
- A title is modeled as a set of requirements (Req.* values).
- A dominates B if requirements(B) is a strict subset of requirements(A).
- Survivors are titles not dominated by another title in the analyzed set.

Quick usage:
- python Tools/title_dominance_analyzer.py summary
- python Tools/title_dominance_analyzer.py edges --title "MVP"
- python Tools/title_dominance_analyzer.py simulate --selected "MVP" "God Complex" "Rambo"
- python Tools/title_dominance_analyzer.py random-sim --players 4 --runs 1000 --seed 42
"""

import argparse
import json
import random
import re
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class TitleDef:
    name: str
    requirements: frozenset[str]


@dataclass(frozen=True)
class AwardedTitle:
    title: TitleDef
    player: int


TITLE_BLOCK_RE = re.compile(
    r"titles\.Add\(new TitleBuilder\(leaders\)(?P<body>.*?)\.Build\(\)\);",
    re.DOTALL,
)

NAME_RE = re.compile(r'\.WithName\("(?P<name>(?:[^"\\]|\\.)*)"\)')
REQ_RE = re.compile(r"Req\.(?P<req>[A-Za-z_][A-Za-z0-9_]*)")


def parse_titles_manager(path: Path) -> list[TitleDef]:
    content = path.read_text(encoding="utf-8")
    titles: list[TitleDef] = []

    for match in TITLE_BLOCK_RE.finditer(content):
        body = match.group("body")
        name_match = NAME_RE.search(body)
        if not name_match:
            continue

        title_name = bytes(name_match.group("name"), "utf-8").decode("unicode_escape")
        requirements = frozenset(REQ_RE.findall(body))
        titles.append(TitleDef(name=title_name, requirements=requirements))

    unique_by_name: dict[str, TitleDef] = {}
    for title in titles:
        unique_by_name[title.name] = title
    return sorted(unique_by_name.values(), key=lambda t: (len(t.requirements), t.name))


def build_dominance_edges(titles: Iterable[TitleDef]) -> list[tuple[str, str]]:
    title_list = list(titles)
    edges: list[tuple[str, str]] = []

    for dominator in title_list:
        for dominated in title_list:
            if dominator.name == dominated.name:
                continue
            if len(dominator.requirements) <= len(dominated.requirements):
                continue
            if dominated.requirements.issubset(dominator.requirements):
                edges.append((dominator.name, dominated.name))

    return sorted(edges)


def apply_dominance_filter(titles: Iterable[TitleDef]) -> tuple[list[TitleDef], list[TitleDef]]:
    title_list = list(titles)
    survivors: list[TitleDef] = []
    removed: list[TitleDef] = []

    for candidate in title_list:
        dominated = False
        for other in title_list:
            if other.name == candidate.name:
                continue
            if len(other.requirements) <= len(candidate.requirements):
                continue
            if candidate.requirements.issubset(other.requirements):
                dominated = True
                break

        if dominated:
            removed.append(candidate)
        else:
            survivors.append(candidate)

    survivors.sort(key=lambda t: (len(t.requirements), t.name))
    removed.sort(key=lambda t: (len(t.requirements), t.name))
    return survivors, removed


def apply_dominance_filter_awarded(
    titles: Iterable[AwardedTitle],
) -> tuple[list[AwardedTitle], list[AwardedTitle]]:
    title_list = list(titles)
    survivors: list[AwardedTitle] = []
    removed: list[AwardedTitle] = []

    for candidate in title_list:
        dominated = False
        for other in title_list:
            if other.title.name == candidate.title.name and other.player == candidate.player:
                continue
            if other.player != candidate.player:
                continue
            if len(other.title.requirements) <= len(candidate.title.requirements):
                continue
            if candidate.title.requirements.issubset(other.title.requirements):
                dominated = True
                break

        if dominated:
            removed.append(candidate)
        else:
            survivors.append(candidate)

    survivors.sort(key=lambda t: (t.player, len(t.title.requirements), t.title.name))
    removed.sort(key=lambda t: (t.player, len(t.title.requirements), t.title.name))
    return survivors, removed


def random_requirements_assignment(
    requirements: Iterable[str],
    player_count: int,
    rng: random.Random,
) -> dict[str, int]:
    if player_count <= 1:
        raise ValueError("--players must be greater than 1")
    return {req: rng.randint(1, player_count) for req in sorted(set(requirements))}


def award_titles_from_assignment(
    titles: Iterable[TitleDef],
    assignment: dict[str, int],
) -> list[AwardedTitle]:
    awarded: list[AwardedTitle] = []
    for title in titles:
        if not title.requirements:
            continue
        req_players = {assignment[req] for req in title.requirements}
        if len(req_players) == 1:
            awarded.append(AwardedTitle(title=title, player=req_players.pop()))
    return awarded


def get_title_subset(all_titles: list[TitleDef], selected_names: list[str]) -> list[TitleDef]:
    by_name = {title.name.lower(): title for title in all_titles}
    missing = [name for name in selected_names if name.lower() not in by_name]
    if missing:
        missing_text = ", ".join(missing)
        raise ValueError(f"Unknown title name(s): {missing_text}")
    return [by_name[name.lower()] for name in selected_names]


def print_titles(titles: Iterable[TitleDef]) -> None:
    for title in titles:
        req_text = ", ".join(sorted(title.requirements))
        print(f"- {title.name} ({len(title.requirements)}): [{req_text}]")


def cmd_summary(all_titles: list[TitleDef]) -> None:
    edges = build_dominance_edges(all_titles)
    print(f"Titles parsed: {len(all_titles)}")
    print(f"Dominance edges: {len(edges)}")

    survivors, removed = apply_dominance_filter(all_titles)
    print(f"Survivors if all titles are considered: {len(survivors)}")
    print(f"Dominated titles removed: {len(removed)}")


def cmd_list(all_titles: list[TitleDef], only_survivors: bool) -> None:
    titles = all_titles
    if only_survivors:
        titles, _ = apply_dominance_filter(all_titles)
    print_titles(titles)


def cmd_edges(all_titles: list[TitleDef], title: str | None) -> None:
    edges = build_dominance_edges(all_titles)
    if title:
        lowered = title.lower()
        edges = [edge for edge in edges if edge[0].lower() == lowered or edge[1].lower() == lowered]
    for dominator, dominated in edges:
        print(f"{dominator} -> {dominated}")


def cmd_simulate(all_titles: list[TitleDef], selected_names: list[str], json_output: bool) -> None:
    selected = get_title_subset(all_titles, selected_names)
    survivors, removed = apply_dominance_filter(selected)

    if json_output:
        payload = {
            "selected": [t.name for t in selected],
            "survivors": [t.name for t in survivors],
            "removed": [t.name for t in removed],
            "survivor_count": len(survivors),
            "removed_count": len(removed),
        }
        print(json.dumps(payload, indent=2))
        return

    print(f"Selected: {len(selected)}")
    print(f"Survivors: {len(survivors)}")
    print(f"Removed: {len(removed)}")
    print("\nSurvivors:")
    print_titles(survivors)
    print("\nRemoved:")
    print_titles(removed)


def cmd_random_sim(
    all_titles: list[TitleDef],
    player_count: int,
    seed: int | None,
    runs: int,
    json_output: bool,
) -> None:
    if runs <= 0:
        raise ValueError("--runs must be greater than 0")

    rng = random.Random(seed)
    all_requirements = {req for title in all_titles for req in title.requirements}
    req_count_by_title = {title.name: len(title.requirements) for title in all_titles}

    if runs == 1:
        assignment = random_requirements_assignment(all_requirements, player_count, rng)
        awarded = award_titles_from_assignment(all_titles, assignment)
        survivors, removed = apply_dominance_filter_awarded(awarded)

        if json_output:
            payload = {
                "players": player_count,
                "seed": seed,
                "runs": runs,
                "requirement_assignment": assignment,
                "awarded": [{"title": a.title.name, "player": a.player} for a in awarded],
                "survivors": [{"title": s.title.name, "player": s.player} for s in survivors],
                "removed": [{"title": r.title.name, "player": r.player} for r in removed],
                "awarded_count": len(awarded),
                "survivor_count": len(survivors),
                "removed_count": len(removed),
            }
            print(json.dumps(payload, indent=2))
            return

        print(f"Players: {player_count}")
        print(f"Seed: {seed if seed is not None else 'random'}")
        print(f"Runs: {runs}")
        print(f"Assigned requirements: {len(assignment)}")
        print(f"Awarded titles before dominance: {len(awarded)}")
        print(f"Survivors: {len(survivors)}")
        print(f"Removed: {len(removed)}")

        print("\nSurvivors:")
        for survivor in survivors:
            req_text = ", ".join(sorted(survivor.title.requirements))
            print(f"- P{survivor.player}: {survivor.title.name} ({len(survivor.title.requirements)}): [{req_text}]")

        print("\nRemoved:")
        for item in removed:
            req_text = ", ".join(sorted(item.title.requirements))
            print(f"- P{item.player}: {item.title.name} ({len(item.title.requirements)}): [{req_text}]")
        return

    total_awarded = 0
    total_survivors = 0
    total_removed = 0
    survivor_counter: Counter[str] = Counter()
    removed_counter: Counter[str] = Counter()

    for _ in range(runs):
        assignment = random_requirements_assignment(all_requirements, player_count, rng)
        awarded = award_titles_from_assignment(all_titles, assignment)
        survivors, removed = apply_dominance_filter_awarded(awarded)

        total_awarded += len(awarded)
        total_survivors += len(survivors)
        total_removed += len(removed)

        survivor_counter.update(s.title.name for s in survivors)
        removed_counter.update(r.title.name for r in removed)

    if json_output:
        payload = {
            "players": player_count,
            "seed": seed,
            "runs": runs,
            "average_awarded_count": total_awarded / runs,
            "average_survivor_count": total_survivors / runs,
            "average_removed_count": total_removed / runs,
            "survivor_frequency": [
                {
                    "title": title,
                    "req_count": req_count_by_title.get(title, 0),
                    "count": count,
                    "rate": count / runs,
                }
                for title, count in sorted(survivor_counter.items(), key=lambda item: (-item[1], item[0]))
            ],
            "removed_frequency": [
                {
                    "title": title,
                    "req_count": req_count_by_title.get(title, 0),
                    "count": count,
                    "rate": count / runs,
                }
                for title, count in sorted(removed_counter.items(), key=lambda item: (-item[1], item[0]))
            ],
        }
        print(json.dumps(payload, indent=2))
        return

    print(f"Players: {player_count}")
    print(f"Seed: {seed if seed is not None else 'random'}")
    print(f"Runs: {runs}")
    print(f"Average awarded titles before dominance: {total_awarded / runs:.2f}")
    print(f"Average survivors: {total_survivors / runs:.2f}")
    print(f"Average removed: {total_removed / runs:.2f}")

    print("\nTop survivor frequencies:")
    for title, count in sorted(survivor_counter.items(), key=lambda item: (-item[1], item[0]))[:20]:
        print(f"- {title} [{req_count_by_title.get(title, 0)} reqs]: {count}/{runs} ({count / runs:.1%})")

    print("\nTop removed frequencies:")
    for title, count in sorted(removed_counter.items(), key=lambda item: (-item[1], item[0]))[:20]:
        print(f"- {title} [{req_count_by_title.get(title, 0)} reqs]: {count}/{runs} ({count / runs:.1%})")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Analyze and simulate title dominance relationships",
        epilog=(
            "Examples:\n"
            "  python Tools/title_dominance_analyzer.py summary\n"
            "  python Tools/title_dominance_analyzer.py list --survivors-only\n"
            "  python Tools/title_dominance_analyzer.py edges --title \"MVP\"\n"
            "  python Tools/title_dominance_analyzer.py simulate --selected \"MVP\" \"God Complex\" \"Rambo\"\n"
            "  python Tools/title_dominance_analyzer.py random-sim --players 4\n"
            "  python Tools/title_dominance_analyzer.py random-sim --players 4 --runs 1000 --seed 42 --json\n\n"
            "Notes:\n"
            "  - In edges output, A -> B means A dominates B (B's requirements are a strict subset of A's).\n"
            "  - random-sim assigns each Req.* to a random player, awards titles whose requirements map to one player,\n"
            "    then applies per-player dominated-title removal.\n"
            "  - Use --runs for frequency stats over many random simulations."
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--titles-file",
        type=Path,
        default=Path("Tracking") / "TitlesManager.cs",
        help="Path to TitlesManager.cs",
    )

    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser(
        "summary",
        help="Show counts for titles, edges, and full-set simulation",
        description=(
            "Parse all titles and print high-level counts:\n"
            "- number of parsed titles\n"
            "- number of dominance edges\n"
            "- survivors/removed when considering all titles"
        ),
        epilog="Example:\n  python Tools/title_dominance_analyzer.py summary",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )

    list_parser = subparsers.add_parser(
        "list",
        help="List parsed titles and their requirements",
        description=(
            "List each parsed title and its requirement set.\n"
            "Use --survivors-only to show only non-dominated titles."
        ),
        epilog=(
            "Examples:\n"
            "  python Tools/title_dominance_analyzer.py list\n"
            "  python Tools/title_dominance_analyzer.py list --survivors-only"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    list_parser.add_argument("--survivors-only", action="store_true", help="List only non-dominated titles")

    edges_parser = subparsers.add_parser(
        "edges",
        help="List dominance edges (A -> B means A dominates B)",
        description=(
            "Print dominance edges between titles.\n"
            "A -> B means A dominates B (requirements(B) is a strict subset of requirements(A))."
        ),
        epilog=(
            "Examples:\n"
            "  python Tools/title_dominance_analyzer.py edges\n"
            "  python Tools/title_dominance_analyzer.py edges --title \"MVP\""
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    edges_parser.add_argument("--title", type=str, help="Filter edges by title name")

    simulate_parser = subparsers.add_parser(
        "simulate",
        help="Run dominance simulation on chosen titles",
        description=(
            "Run subset simulation on explicitly selected titles.\n"
            "Removes titles dominated by another selected title."
        ),
        epilog=(
            "Examples:\n"
            "  python Tools/title_dominance_analyzer.py simulate --selected \"MVP\" \"God Complex\" \"Rambo\"\n"
            "  python Tools/title_dominance_analyzer.py simulate --selected \"MVP\" \"God Complex\" --json"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    simulate_parser.add_argument(
        "--selected",
        type=str,
        nargs="+",
        required=True,
        help='Selected titles, e.g. --selected "MVP" "God Complex" "Rambo"',
    )
    simulate_parser.add_argument("--json", action="store_true", help="Output simulation as JSON")

    random_sim_parser = subparsers.add_parser(
        "random-sim",
        help="Assign each requirement to a random player, award titles, then remove dominated titles per player",
        description=(
            "Random simulation mode.\n"
            "1) Assign each Req.* to a random player in [1, players].\n"
            "2) Award titles whose requirement set maps to a single player.\n"
            "3) Remove dominated titles per player.\n"
            "4) If --runs > 1, report frequency statistics."
        ),
        epilog=(
            "Examples:\n"
            "  python Tools/title_dominance_analyzer.py random-sim --players 4\n"
            "  python Tools/title_dominance_analyzer.py random-sim --players 4 --seed 42\n"
            "  python Tools/title_dominance_analyzer.py random-sim --players 4 --runs 1000\n"
            "  python Tools/title_dominance_analyzer.py random-sim --players 4 --runs 1000 --seed 42 --json"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    random_sim_parser.add_argument(
        "--players",
        type=int,
        required=True,
        help="Number of players (must be > 1)",
    )
    random_sim_parser.add_argument(
        "--seed",
        type=int,
        help="Optional RNG seed for reproducible runs",
    )
    random_sim_parser.add_argument(
        "--runs",
        type=int,
        default=1,
        help="Number of random simulations to run (default: 1)",
    )
    random_sim_parser.add_argument("--json", action="store_true", help="Output simulation as JSON")

    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()

    if not args.titles_file.exists():
        raise FileNotFoundError(f"Could not find titles file: {args.titles_file}")

    all_titles = parse_titles_manager(args.titles_file)
    if not all_titles:
        raise RuntimeError("No titles could be parsed from the input file")

    if args.command == "summary":
        cmd_summary(all_titles)
    elif args.command == "list":
        cmd_list(all_titles, args.survivors_only)
    elif args.command == "edges":
        cmd_edges(all_titles, args.title)
    elif args.command == "simulate":
        cmd_simulate(all_titles, args.selected, args.json)
    elif args.command == "random-sim":
        cmd_random_sim(all_titles, args.players, args.seed, args.runs, args.json)


if __name__ == "__main__":
    main()