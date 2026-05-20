# D358 - Mobile App Redesign Assessment

Date: 2026-05-20

## Assessment

D357 improved overflow and stacking, but it kept the desktop sidebar concept alive on mobile. Owner testing confirmed the public home navbar and in-app topbar/sidebar compromise were still not usable enough on phones. The app needed a real mobile navigation model rather than more horizontal scrolling.

The visual style also had a repeated blue-left-card motif that made normal product cards feel like highlighted debug panels. That motif should not be the default language for cards or navigation.

## Completed task

Implemented the first full mobile shell redesign:

- Public home now has a mobile menu button and full-width mobile menu.
- Authenticated app hides the desktop sidebar below the desktop breakpoint.
- Authenticated app now has a mobile top bar with current area context.
- Authenticated app now has a bottom nav for Home, Studies, Study, Directory, and More.
- More opens a mobile menu sheet for Team, Exports, Settings, and Sign out.
- Selected-study pages now have a compact mobile area switcher for Hub, Setup, Collect, Results, and Waves.
- Desktop sidebar and selected-study cards remain available on desktop.
- Blue-left-card and inset-left selected motifs were replaced with neutral cards and softer selected states.

## Verification

Passed:

- Web production build.
- `git diff --check` with only CRLF normalization warnings.

Deployment pending in this session.

## Remaining risk

- This is still a CSS/component implementation pass, not a full device screenshot audit.
- The next mobile-quality slice should run screenshot review at 360px, 390px, 430px, tablet, and desktop after owner testing.
- Vite still reports the existing large-chunk warning during production build.
