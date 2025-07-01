## Usage

Simply press the assigned button (default `K`), and your player will die.

Your current velocity is transferred onto the corpse, so if you sprint-jump, the corpse will keep that momentum.

## Configuring

You can change the key in the input settings.

Additionally, you can customize the details of how you die.

By default, the cause of death will be `Unknown`, but you can change it in the config.

The ragdoll that is spawned is specified separately, but by default it follows the cause of death.

### Enemies and their causes of death:

#### Inside:

| Enemy                     | Causes of death       | Player ragdolls |
|---------------------------|-----------------------|-----------------|
| Hoarding bug              | Mauling               | Normal          |
| Nutcracker                | Gunshots _or_ Kicking | Normal          |
| Butler _and_ Mask Hornets | Stabbing              | Normal          |
| Coil head                 | Mauling               | Spring          |
| Snare flea                | Suffocation           | Normal          |
| Thumper                   | Mauling               | Normal          |
| Hygrodere                 | Unknown               | _Special_       |
| Spore Lizard              | Mauling               | Normal          |
| Barber                    | Snipped               | Sliced          |
| Bracken                   | Strangulation         | Normal          |
| Bunker Spider             | Mauling               | _Special_       |
| Ghost Girl                | Unknown               | HeadBurst       |
| Jester                    | Mauling               | Normal          |
| Maneater                  | Mauling               | Normal          |
| Kidnapper Fox _(Unused)_  | Mauling               | HeadGone        |
| Lasso Man _(Unused)_      | Strangulation         | Normal          |

#### Outside (Night):

| Enemy                    | Causes of death              | Player ragdolls   |
|--------------------------|------------------------------|-------------------|
| Baboon Hawk              | Unknown                      | Normal            |
| Earth Leviathan          | Unknown                      | None              |
| Eyeless Dog              | Mauling                      | Normal            |
| Forest Keeper            | Crushing                     | None              |
| Old Bird                 | Crushing, Blast _or_ Burning | Normal _or_ Burnt |

#### Outside (Day):

| Enemy           | Causes of death              | Player ragdolls   |
|-----------------|------------------------------|-------------------|
| Circuit Bees    | Electrocution                | Electrocuted      |
| Giant Sapsucker | Stabbing                     | Pieces            |

#### Other game elements:

| Game element              | Causes of death              | Player ragdolls   |
|---------------------------|------------------------------|-------------------|
| Fan (Factory entrance)    | Fan                          | HeadBurst         |
| Water                     | Drowning                     | Normal            |
| Quicksand (Rainy Puddles) | Suffocation                  | None              |
| Players (Friendly Fire)   | Bludgeoning                  | _Special_         |
| Fall Damage               | Gravity                      | Normal            |
| Landmines                 | Blast                        | Normal            |
| Turrets                   | Gunshots                     | Normal            |
| Trap                      | Crushing                     | Normal            |
| Cruiser                   | Blast, Crushing _or_ Inertia | Burnt _or_ Normal |
| Jetpack                   | Gravity _or_ Blast           | Normal            |
| Out of bounds             | Unknown                      | None              |

**Note: Special ragdolls are not supported**