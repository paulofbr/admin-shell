export default {
  "ReportingPluginFrontend": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/plugins/reporting.json",
    "output": {
      "target": "/home/paulo/admin-shell/plugins/ReportingPlugin/FrontEnd/src/generated/api.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": true,
      "override": {
        "mutator": {
          "path": "/home/paulo/admin-shell/plugins/ReportingPlugin/FrontEnd/src/api/orval-client.ts",
          "name": "httpClient"
        },
        "fetch": {
          "includeHttpResponseReturnType": false
        }
      }
    }
  }
}
