# navigay: App Flow

Doc 3 of 6 · Draft 4 · 24 Nov 2026

The final low-fi screens are in `UI package/App flow screens`. [all-screens.png](../UI%20package/App%20flow%20screens/all-screens.png) is the whole set. This doc follows those pictures. Feature IDs (F1–F15) refer to the [PRD](01-PRD.md). Colours and type are in the [UI/UX Brief](04-ui-ux-brief.md). Stack: [TRD](02-TRD.md). Map: [Map Data Schema](05-map-data-schema.md). Build order: [Implementation Plan](06-implementation-plan.md).

This doc wins for screens, path, and on-screen words. v1 has one job: pick a place, then follow the lane from where the user is stuck to its nearest door. There is no account, no favourites, no 2D map, and no question that asks Boys, Girls, or Staff. **Where to?** is a search field.

**Stuck** means the phone has locked the user to the centre of one lane, they are outside every building, the reported position error is about 25 m or better, and that lock has held for about 2 seconds. A loose GPS point is not stuck. The camera does not have to be working. About 25 m only lets the guide start. Dots use the tighter levels in the PRD.

---

## 1. The path

```
S2 Where to?
  → S1 Permissions, if location was denied or Precise Location is off
  → S3 Place card
      → S1 Permissions, if camera or location is still missing
      → S4 Ready
          → S5 Guide
              → S6 Arrival
                  → S2
          → Back returns to S3
      → X returns to S2
```

S5 End returns to S2. There is no confirm screen. The pictures do not have one.

The app remembers the route only while the process is alive. If iOS kills it, the next open is S2. There is no saved walk and no favourites.

S2 can be browsed and searched with no permission. Focusing **Where to?** asks for location when it is missing, so **Near you** can appear once the user is stuck. The camera opens on S4, not on the list. The first system prompt is the iOS dialog. S1 is for a denial, or for Precise Location.

---

## 2. The six screens

| Screen | Picture | When | What you can do |
|---|---|---|---|
| S1 Permissions | [S1-permissions.png](../UI%20package/App%20flow%20screens/S1-permissions.png) | Location or camera was denied, or Precise Location is off | Open Settings |
| S2 Where to? | [S2-place-list.png](../UI%20package/App%20flow%20screens/S2-place-list.png) | Launch, after Done, after End | Search, scroll suggestions, tap a place |
| S3 Place card | [S3-place-card.png](../UI%20package/App%20flow%20screens/S3-place-card.png) | A row is tapped | Close, or Go |
| S4 Ready | [S4-ready.png](../UI%20package/App%20flow%20screens/S4-ready.png) | Go, and the permissions are on | Wait until stuck, or Back |
| S5 Guide | [S5-guide.png](../UI%20package/App%20flow%20screens/S5-guide.png) | The user is stuck and outside. The camera may be dark | Walk, or End |
| S6 Arrival | [S6-arrival.png](../UI%20package/App%20flow%20screens/S6-arrival.png) | The door checkpoint is reached | Done |

If `graph.json` did not load, the build is broken and the app stops. That is not one of these screens.

---

## 3. S1 Permissions

[S1-permissions.png](../UI%20package/App%20flow%20screens/S1-permissions.png)

A full black screen. One sentence, and a blue **Open Settings** button at the bottom. The same screen, with the sentence swapped:

| Missing | Sentence on the screen |
|---|---|
| Camera | navigay uses the camera to show directions on the path. |
| Location | navigay uses your location to guide you around GHS. |
| Precise Location | iOS is reporting an accuracy worse than 1,000 m. Turn on Precise Location for navigay. |

The camera and location sentences match the Info.plist strings in the TRD. Precise Location uses this screen when the reported accuracy stays worse than 1,000 m.

If camera or location is denied, S3 stays open. S2 stays usable.

---

## 4. S2 Where to? (F1)

[S2-place-list.png](../UI%20package/App%20flow%20screens/S2-place-list.png)

The picture shows the title and the type groups. The title is the search field. Its placeholder is **Where to?**. Tapping it opens the keyboard. The rows under it are the suggestions.

A row is the place name and a chevron. It does not show an access tag, a door ID, or a distance. The minutes stay on the place card, so the list and the card do not show two different times.

| What is in the field | What the rows are |
|---|---|
| Empty, and the user is stuck | **Near you.** Every place, nearest walking route from the stuck point first. The order is computed once for that stuck point, and again only when the stuck point is replaced |
| Empty, and the user is not stuck | The type groups below. No **Near you** heading. A loose fix inside a building stays in this row |
| Text | Matching names from the whole list. B2 comes before B10. Group headings hide |

Groups, in this order, and only if that group has at least one place:

1. Blocks
2. Food
3. Offices and services
4. Gates
5. Sports

The rows in the picture are samples, one or two per group: B2, B3, Quess Mess, Warden office, Underpass entrance, Pool. The live rows come from `graph.json`. A place appears when it is in the file. On 24 Nov 2026 the file has B1–B3, B5–B10, G2–G4, Quess Mess, BlueDove Mess, and Underpass entrance. Warden office and Pool are on the wireframe so those groups are visible. They are not in the file yet. The picture does not yet draw the caret or the **Near you** heading. This section is the screen.

---

## 5. S3 Place card (F1)

[S3-place-card.png](../UI%20package/App%20flow%20screens/S3-place-card.png)

A sheet over a dimmed S2. The picture uses Warden office.

| On the sheet | Example |
|---|---|
| Name | Warden office |
| Type, under the name | Office |
| Time box, large line | About 6 min |
| Time box, small line | Nearest entrance |
| Button | Go |
| Corner | X, back to S2 |

The type word is Block, Food, Office, Gate, or Sports.

Until the phone has stuck the user to a path, the large line reads **Step outside to see the time.** A loose location inside a building is not enough. Go still works.

Once the user is stuck, `About 6 min` is the walk from that stuck point to the nearest door, at 1.2 m/s until this walk has its own pace (TRD section 8). Go keeps that route. The guide opens on the same minutes. The app picks the nearest door. A mess with three open sides is still one row.

---

## 6. S4 Ready (F3)

[S4-ready.png](../UI%20package/App%20flow%20screens/S4-ready.png)

The camera is on. **Back** is a pill at the top left. One dark pill in the middle reads **Finding you…** The Immersal logo sits at the bottom left. The app keeps trying. There is no second Go. A dark or lost camera does not keep this screen up.

The route is calculated when Go is pressed, before this screen waits for a stuck lock or a working camera. If no location fix has arrived, it is calculated once on the first fix. If the user is inside a building or the phone cannot snap them to a lane, it is calculated once when the first legal snap exists, still on this screen, not on a timer. If that calculation finds no path, the pill below says so at once. When the stuck lock completes, keep the route if the stuck point is on it. If the stuck point is off it, replace the route once from that point, still on this screen. Minutes wait for that stuck point, and they use the settled route. The guide does not calculate the first route, and the router does not run every moment.

Directions start when the user is stuck and outside. They do not start because the camera looks healthy.

These lines replace `Finding you…`. They are the pills on the S4 picture:

| Case | Pill |
|---|---|
| Still warming up, not stuck | Finding you… |
| Inside a block that has a place name | You're inside B3. Step outside to start. |
| Inside a block with no place name | You're inside a building. Step outside to start. |
| Outside, but the phone cannot lock a lane | Step into the open. |
| No route to that place | Can't find a way there. |

The block name comes from the outline's place. Back returns to S3 and drops this attempt.

`Can't find a way there.` means the router found no path from the snapped position. The app does not draw a straight line across a building. `Step into the open.` is for a fix that is still too loose to lock a lane, including a reported error worse than about 25 m. That 25 m bar is not the dot bar.

---

## 7. S5 Guide (F4, F5, F6, F7, F8)

[S5-guide.png](../UI%20package/App%20flow%20screens/S5-guide.png)

This picture is **Daytime Full**. The route is the shortest walk on GHS lanes from the stuck point to the nearest door. Every access tag is allowed. It was calculated before this screen opened. This screen does not calculate it again. A new route is calculated only after the person has left the route (section 8).

**Turn card, top.** A turn arrow and one line: `Turn left at B2 · 30 m`. The same card says `Continue straight · 80 m` or `Arrive at Quess Mess · 12 m` when that is the next move. The landmark is the junction's `landmark`, or the nearest place name.

The `30 m` is the distance along the route to the next checkpoint. Above 50 m it rounds to 5 m. Below 50 m it shows 1 m steps. It goes down as you walk. It goes up only if you walk back along the route by more than about 3 m.

**On the path.** Blue dots run along the lane centre from your feet toward the next checkpoint. They get smaller with distance. A sign sits over the trail, with a line down to it: `2/4 · Turn left · B2 · in 30 m`. The count includes the door, so 4/4 is the door when there are three turns before it.

Reaching a checkpoint (within about 8 m, or once you have passed it) vibrates once. The next dots appear. A vibration means a checkpoint or the arrival, not a re-route.

**Trip bar, bottom.** The picture shows `320` metres left, `4` minutes left, `9:45` arrival, and **End**. The opening minutes are the same number the place card showed, because both use the route from the same stuck point. Arrival time is the phone clock plus the minutes left. The Immersal logo is above the bar, on the left, clear of the dots and the turn card.

**End** is that button. It returns to S2. It does not open another sheet.

**Other levels use this same screen.** Short keeps the card, the sign, and the bar, and shortens the dots to about 10 m. Hold hides the dots and the checkpoint sign. The turn card and the trip bar stay. A **flat 2D on-screen arrow** points to the next checkpoint (compass + position). It is not a 3D floor arrow. Full is the frame that was drawn. Moving up a level waits 2 seconds. Moving down happens at once. Hold is also the dark, the lost camera, and the phone pointed more than about 60° below the horizon. Dots come back only when the camera is fine and the level is Short or Full. A weak or dark camera stays on this screen with the flat arrow. It does not return to S4.

On Hold the turn card may add one sentence:

| Weak part | Extra sentence |
|---|---|
| Position error is outside Short (worse than about 12 m) | Walk a few steps. |
| Position is already within about 12 m, and the heading is the weak part | Wave the phone in a figure-eight. |
| Only the camera is weak: dark, tracking lost, or phone lowered | No extra sentence. The flat arrow is the direction |

**Edge arrow.** In Full or Short, when the next dots are off-screen, an arrow on the screen edge points toward them. Hidden in Hold (the flat arrow is already on screen).

If the snapped position falls inside a block during S5, the app holds the current route and the middle line becomes `Step outside.` It does not route through the building.

---

## 8. Re-route (F11)

Leaving the route means you are about 20 m off it, and you have moved at least about 10 m since the last route. Only then does S5 compute a new route from where you are. It does not do this when the screen opens, and it does not do it every moment.

There is no question and no extra vibration. The turn card and the dots change. The new minutes use the new stuck point, so the card and the guide still share one answer.

---

## 9. S6 Arrival (F12, F13)

[S6-arrival.png](../UI%20package/App%20flow%20screens/S6-arrival.png)

A card at the top of the camera:

- The place name, large. The picture says Warden office.
- `4 min · 320 m`
- A blue **Done** button

Done returns to S2. One vibration when the card appears. The Immersal logo stays at the bottom left.

The underpass uses this same screen and adds `This is the entrance. navigay stops here.` It stops at the entrance. There is no second step, and nothing in the flow continues through the underpass or out to the academic campus.

Until the main gate exists, the underpass is the only `start=yes` place in the data. That flag is the connectivity root for the map check. It is not where the user has to stand, and it is not the map zero. The app does not skip S4 because of it. A walk can start on any lane the phone has stuck the user to.

---

## 10. What these screens never show

- Boys, Girls, or Staff. The tags stay in the files. S2 lists every place. S5 may use every lane.
- A 2D map, a star, a share button, voice, a step counter, favourites, or another stop. Search lives in the **Where to?** field.
- A door ID, a lane ID, or a metres-accuracy claim.
- Directions inside a block, inside the underpass, or outside GHS.
- An End confirmation sheet. End on S5 goes straight back to S2.

---

## 11. v2, not in these pictures

v2 inserts one question before S2: Boys, Girls, or Staff. The suggestions and the router then follow the access rule in the [Map Data Schema](05-map-data-schema.md). S3 through S6 stay. Favourites, voice, and the 2D map are extra screens, specified when v2 starts.
