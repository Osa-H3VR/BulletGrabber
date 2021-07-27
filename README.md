# BulletKinesis
***u grab bulets***

## Description:
Do you hate the feeling of just dropping all bullets to the ground and having to pick them up?

Do you use Index and your thumb just hurts on limited ammo runs?

Do you want to have Psychokinesis power?

Boy, I have the thing for you!

## Usage

Just point the grab lasor to the bulet and grab it, all nearby bullets will start entering your palm.

## Installation
Recommended: Use thunderstore to install: https://h3vr.thunderstore.io/package/OsaPL/BulletKinesis/

Requires: Deli 4.1
Manual:
1. Download the release `.deli` file
2. Open `.deli` file as archive
3. Extract it to `[profile]/BepInEx/plugins/BulletKinesis/`, create the folder if needed

## Configurating:
Open: `Steam\steamapps\common\H3VR\Deli\configs\osa.bulletkinesis.cfg` with a notepad to configure. 

### HandMode
You can choose if the hand should be able to use the grab.
Valid options are: `both`, `left` or `right`.
### BulletGrabMode
Three modes of bullet grabbing:
- `firstTheSame`: Default. It picks up all bullets ot the selected type first, then (if needed) fill with the rest that is also compatible
- `closest`: It picks up bullets closest to you, disregarding the type
- `onlyTheSame`: Only picks up bullets of the same type

### Delay
Defines delay (in milliseconds) between each bullet grab.
The lowest it can go is your frame time times two.

### Range
Defines range (in meters) of grab. Setting it higher than default will disable TnH scoring.
