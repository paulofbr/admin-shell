export default {
  "adminShell": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/adminshell.json",
    "output": {
      "target": "src/generated/api/adminshell.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": true,
      "override": {
        "mutator": {
          "path": "./packages/admin-shell-ui/src/http-client/orval-mutator.cjs",
          "name": "httpClient"
        },
        "fetch": {
          "includeHttpResponseReturnType": false
        }
      }
    }
  }
}
