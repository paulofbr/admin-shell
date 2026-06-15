export default {
  "OrderCreationPluginFrontend": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/plugins/order-creation.json",
    "output": {
      "target": "/home/paulo/admin-shell/plugins/OrderCreationPlugin/FrontEnd/src/generated/api.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": true,
      "override": {
        "mutator": {
          "path": "../../../frontend/packages/admin-shell-http-client/src/index.ts",
          "name": "httpClient"
        },
        "fetch": {
          "includeHttpResponseReturnType": false
        }
      }
    }
  }
}
