# Cube Blast

A little cube-blast match-3 puzzle game, made in Unity — tap groups of same-colored cubes to clear them, chain together rockets and TNT for bigger combos, and clear each level's obstacles before you run out of moves.

## How to play

1. Open the project in Unity (**6000.3.10f1**).
2. If it's your first time opening it, run **Dream Games → Setup Project (Build Scenes and Prefabs)** from the menu bar. This generates all the placeholder art and builds the two scenes — no art assets are included, everything's drawn procedurally in code.
3. Open `MainScene` and hit Play.

## What's in it

- Tap a group of 2+ same-colored cubes to blast them. Groups of 4-5 turn into a **rocket** (clears a row or column), groups of 6+ turn into a **TNT** (clears a 5x5 area).
- Combine special items for bigger effects: two rockets cross in a plus shape, a rocket + TNT fires a thick cross of rockets, two TNTs blow up a 7x7 area.
- Obstacles to clear along the way:
  - **Vase** — takes a couple of hits, falls like a normal cube.
  - **Stone** — only clears from rocket/TNT explosions, not regular blasts.
  - **Chalice Box** — a 2x2 obstacle with a locked door; break the door, then keep hitting it to collect chalices until it's fully cleared.
- 10 hand-built levels, each with its own move limit and mix of obstacles.
- Win a level and it celebrates; run out of moves and you get a retry/quit popup.

## Notes

Since no art assets came with this, every sprite (cubes, rockets, obstacles, the background) is generated procedurally at editor time by `Assets/Editor/ProjectSceneBootstrapper.cs`. All the animation (falling, popping, camera shake, etc.) is done with a small custom coroutine-based tweening helper rather than Unity's Animator or Physics.
