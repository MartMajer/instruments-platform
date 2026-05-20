# D358 Mobile App Redesign Design

Date: 2026-05-20

## Goal

Replace the rushed mobile topbar/sidebar compromise with a real mobile navigation model and remove the blue-left-card visual motif across the app.

## Design

Desktop keeps the current sidebar and selected-study route cards. Mobile gets a separate shell:

- public home uses a compact SaaS header with a menu button and full-width menu sheet;
- authenticated app hides the desktop sidebar on mobile;
- authenticated app adds a mobile top bar with current route context;
- authenticated app adds a bottom nav for Home, Studies, Study, Directory, and More;
- More opens a menu sheet for Team, Exports, Settings, and sign-out;
- selected study pages hide the desktop five-card route nav on mobile and use a compact switcher.

The visual cleanup removes the blue-left-card motif by replacing left accent borders and inset blue selection shadows with neutral cards, soft borders, and restrained selected states. Blue remains only as functional selected/focus/accent color.

## Acceptance criteria

- Mobile app routes do not render the desktop sidebar inline.
- Home mobile nav is not a crushed horizontal/stacked navbar.
- Study route navigation does not consume the first screen on phones.
- Blue-left-border cards are removed from the normal app shell and selected-study navigation.
- Desktop sidebar and desktop selected-study cards remain available above desktop breakpoint.
- Build and staging deployment pass before calling the slice done.
