# Design: Gallery lightbox navigation

**Date:** 2026-07-13  
**Status:** Approved — implementing

## Problem

On event/souvenirs galleries, viewing the next photo requires closing the lightbox and clicking another thumbnail.

## Solution

Enhance `EventMediaGallery` lightbox with previous/next controls, circular wrap, keyboard (`←`/`→`/`Esc`), and a discrete counter (`3 / 12`).

## Scope

- In: public gallery lightbox UX + FR/EN i18n labels
- Out: filmstrip, swipe gestures, admin multi-select

## Implementation notes

- Replace `active: EventMedia | null` with `activeIndex: number | null`
- No backend/API changes
- Hide prev/next when fewer than 2 media items
