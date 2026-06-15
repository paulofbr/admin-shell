# Generated API client

This folder is generated from the AdminShell OpenAPI document.
- `openapi/adminshell.json` is the full backend contract.
- `openapi/plugins/*.json` contains one filtered contract per plugin API.
- `api/adminshell.ts` is the generated backend client.
- `plugins/*.ts` are generated plugin clients for the main AdminShell frontend.
- Plugins with both `Backend` and `FrontEnd` also get their own generated client under `plugins/<PluginName>/FrontEnd/src/generated/api.ts`.
- `index.ts` re-exports the generated clients for application code.

Regenerate with:

```bash
npm run generate:api-client
```

Do not edit generated files manually.