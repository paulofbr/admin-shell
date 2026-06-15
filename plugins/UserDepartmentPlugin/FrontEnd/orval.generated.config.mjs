export default {
  "UserDepartmentPluginFrontend": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/plugins/user-department.json",
    "output": {
      "target": "/home/paulo/admin-shell/plugins/UserDepartmentPlugin/FrontEnd/src/generated/api.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": true,
      "override": {
        "mutator": {
          "path": "/home/paulo/admin-shell/plugins/UserDepartmentPlugin/FrontEnd/src/api/orval-client.ts",
          "name": "httpClient"
        },
        "fetch": {
          "includeHttpResponseReturnType": false
        }
      }
    }
  }
}
