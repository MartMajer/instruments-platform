# Mobile Responsive Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the current private-beta app more usable on mobile and narrow tablet widths without a full navigation redesign.

**Architecture:** Use shared CSS overrides in `apps/web/src/routes/app.css` so the shell, product navigation, workflow paths, panels, forms, and tables respond consistently. Avoid Svelte component rewrites unless CSS cannot solve a layout break.

**Tech Stack:** SvelteKit, global app CSS, existing product components.

---

### Task 1: Shared responsive shell and navigation

**Files:**
- Modify: `apps/web/src/routes/app.css`

**Steps:**
- Add mobile-first overflow guards for page, app shell, main content, cards, forms, and technical values.
- At widths below `1024px`, make `.app-sidebar` a sticky compact top region.
- Convert `.product-nav` and `.product-nav__list` into horizontally scrollable compact groups.
- Hide low-value nav descriptions on smaller screens.
- Keep desktop behavior unchanged above `1024px`.

**Verification:**
- Run frontend production build or Svelte check if available.
- Run `git diff --check`.

### Task 2: Shared product surface responsiveness

**Files:**
- Modify: `apps/web/src/routes/app.css`

**Steps:**
- Collapse selected-study nav and summary rows safely on tablet/phone widths.
- Make setup/collection/results/waves path cards use horizontal snap on narrow tablets and one-column cards on phones.
- Convert common action rows, form grids, record grids, stats, and data tables into mobile-safe layouts.
- Ensure long IDs/emails/generated names wrap instead of forcing viewport overflow.

**Verification:**
- Run frontend production build or focused route test if feasible.
- Run `git diff --check`.

### Task 3: Handoff and commit

**Files:**
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
- Record the responsive cleanup assessment, changed behavior, verification, and remaining risk.
- Commit the responsive cleanup slice.

**Verification:**
- `git status --short --branch` shows only intended staged files before commit.
