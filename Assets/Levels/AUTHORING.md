# BulkoGame level authoring

Create a level with **BulkoGame > Levels > Create New Level**. It creates a
`LevelDefinition`, a scene scaffold, gameplay camera, `LevelBootstrap`,
`LevelSceneBindings`, and player/basket anchors.

Use **BulkoGame > Levels > Configure Shared Setup** after adding authored
sock points, cat routes, or trap zones. It connects the shared setup and
collects those components from the `Authoring` hierarchy without moving them.

For the authored Level_02 apartment, **BulkoGame > Levels > Generate Sock
Candidates** bakes its planned 32 editable sock points from the named floor
regions after checking Bulka-sized clearance against solid scene colliders.
It deliberately refuses to replace existing manually edited points.

The level layout stays hand-authored: floors, walls, furniture placement,
colliders, spawn points, routes, route sequence, and trap zones belong to the
scene. Shared furniture building blocks live in `Assets/Levels/Shared/Furniture/`.

Run **BulkoGame > Levels > Validate Current Level** before runtime QA. It
checks scene wiring, authored static geometry around sock points, route
segments, shortcut approaches, trap-zone placement, distribution rules, and
audio-listener setup. Geometry checks are conservative authoring checks only:
they do not prove full Bulka reachability, dynamic cat traversal,
CatShortcutBlocker runtime collision filtering, or complete level playability.
Confirm those behaviors with Play Mode/runtime smoke checks.
