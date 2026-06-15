# Vue 3 + TypeScript + Vite

This frontend is the Admin Shell Vue 3 SPA.

Plugin frontend source should follow the project convention:

- `.vue` files for new pages/components
- `.ts` files for non-UI code such as services, types, permissions, and composables
- a compiled `index.js` runtime entry in the packaged plugin `frontend/` folder

Permissions are exported by the plugin frontend entry, not declared in the plugin manifest.
