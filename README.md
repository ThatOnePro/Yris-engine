# Yris Engine (.NET)

## Quick benchmark (NPS + ms)

The UCI app supports a simple `bench` command so you can compare speed changes quickly.

From `Engine.Uci`, run the engine and send:

- `position startpos`
- `bench 5` (depth 5, 1 run)
- `bench 5 3` (depth 5, 3 runs, prints average)

Example output includes:

- `nodes` = perft node count
- `time` = elapsed milliseconds
- `nps` = nodes per second
- `avgTime` / `avgNps` for multi-run benches

You can also benchmark any custom position first:

- `position fen <your-fen-here>`
- `bench 5 3`

