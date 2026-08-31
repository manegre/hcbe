# Design: Event attachments + gallery on edit

**Date:** 2026-07-13  
**Status:** Approved — implementing

## Goals

1. Pièces jointes sur événements (miroir annonces)
2. Galerie souvenirs aussi sur la page admin Modifier
3. Après création → redirect edit pour ajouter médias/PJ

## Backend

- `EventAttachment` entity + `EventAttachments` table
- `EventDto.Attachments`
- `POST/DELETE /api/events/{id}/attachments`
- Cleanup files on event delete

## Frontend

- API client methods
- Admin: attachments manager on view/edit; pending on create then upload
- Public: documents section on event detail
- Edit page: `EventGalleryManager` under form
