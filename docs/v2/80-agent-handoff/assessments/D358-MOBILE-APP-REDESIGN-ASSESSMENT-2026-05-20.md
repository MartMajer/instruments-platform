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
- Follow-up CSS audit removed residual source declarations for 3px accent-left borders and inset accent shadows.

## Verification

Passed:

- Web production build.
- `git diff --check` with only CRLF normalization warnings.
- Source audit found no remaining `inset 3px`, `inset 2px`, `border-left: 3px`, `border-left-width: 3px`, `inset 0 3px`, or `inset 0 -2px` declarations in `app.css`.

Staging deployment passed:

- Deployed app commit: `30ba356`.
- VPS redeploy evidence: `/tmp/d358-mobile-nav-vps-release-20260520`.
- VPS release evidence: `/tmp/d358-mobile-nav-vps-release-20260520/release-evidence`.
- Public follow-up checks passed: API ready 200, web root 200, `/app` 200.
- Authenticated remote smoke was skipped.
- Web image: `sha256:a6909c0e3df11158c6406cc646d7e0d77e6c76c1dbb5451c4b3f3d12303ab341`.
- API image: `sha256:cc06bb1dde41cb9a14a3c74969c52cd68b3e6937e65f3691ee5db7a20e157ed3`.
- Worker image: `sha256:319044a1483cff5d860d769a5791172739dd6045f64ce4003130041c13e9ea62`.

## Remaining risk

- This is still a CSS/component implementation pass, not a full device screenshot audit.
- The next mobile-quality slice should run screenshot review at 360px, 390px, 430px, tablet, and desktop after owner testing.
- Vite still reports the existing large-chunk warning during production build.
