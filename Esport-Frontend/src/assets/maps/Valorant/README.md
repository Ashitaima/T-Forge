# Valorant map images

This directory is read by `mapImageUrl` in `src/constants/gameMaps.ts` via
`import.meta.glob("../assets/maps/*/*.jpg", { eager: true, query: "?url" })`.

It is currently empty. Until the files below are added, every Valorant map
tile in the draft falls back to its text label (the same `onError` path used
for hero portraits) — that is expected, not a bug.

Drop exactly these files here, named to match `Common/GameMaps.cs` verbatim
(case-sensitive, `.jpg`, no other extension):

- `Abyss.jpg`
- `Ascent.jpg`
- `Haven.jpg`
- `Lotus.jpg`
- `Split.jpg`
- `Summit.jpg`
- `Sunset.jpg`

Keep each file under roughly 200 KB — these are draft-board tiles (16:9), not
wallpapers. See `tools/fetch-map-images.ps1` for a script to fetch them once
its source-URL table is filled in (deliberately left empty — see that file's
header comment for why).
