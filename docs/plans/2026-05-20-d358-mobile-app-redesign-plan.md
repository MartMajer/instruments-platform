# D358 Mobile App Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rework mobile navigation and app-wide visual primitives so the current product is usable on phone widths without preserving the disliked blue-left-card motif.

**Architecture:** Add mobile-only navigation structures in the shared shell and selected-study nav components while keeping desktop markup intact. Centralize visual and responsive behavior in `apps/web/src/routes/app.css`.

**Tech Stack:** SvelteKit, Svelte 5 component state, global app CSS, existing lucide icons.

---

### Task 1: Mobile app shell

**Files:**
- Modify: `apps/web/src/lib/components/AppShell.svelte`
- Modify: `apps/web/src/routes/app.css`

**Steps:**
- Add mobile top bar for authenticated app routes.
- Add bottom mobile nav for Home, Studies, Study, Directory, and More.
- Add More menu sheet with Team, Exports, Settings, and sign-out.
- Hide desktop sidebar below the desktop breakpoint for product routes.

**Verification:**
- Web production build.

### Task 2: Public and selected-study mobile nav

**Files:**
- Modify: `apps/web/src/routes/+page.svelte`
- Modify: `apps/web/src/lib/components/SelectedSeriesWorkspaceNav.svelte`
- Modify: `apps/web/src/routes/app.css`

**Steps:**
- Add public home mobile menu button and menu sheet.
- Add selected-study mobile switcher.
- Hide selected-study desktop nav cards on phone widths.

**Verification:**
- Web production build.

### Task 3: Visual system cleanup and handoff

**Files:**
- Modify: `apps/web/src/routes/app.css`
- Create: `docs/v2/80-agent-handoff/assessments/D358-MOBILE-APP-REDESIGN-ASSESSMENT-2026-05-20.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
- Remove blue-left-card selected/section motifs from app shell and selected-study cards.
- Keep selected/focus states clear but less decorative.
- Update handoff docs with verification and deployment notes.

**Verification:**
- Web production build.
- `git diff --check`.
- Staging deploy and VPS release checks when owner approves deployment.
