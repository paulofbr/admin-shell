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
  },
  "order-creationPlugin": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/plugins/order-creation.json",
    "output": {
      "target": "src/generated/plugins/order-creation.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": false,
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
  },
  "reportingPlugin": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/plugins/reporting.json",
    "output": {
      "target": "src/generated/plugins/reporting.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": false,
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
  },
  "user-departmentPlugin": {
    "input": "/home/paulo/admin-shell/frontend/src/generated/openapi/plugins/user-department.json",
    "output": {
      "target": "src/generated/plugins/user-department.ts",
      "client": "axios",
      "baseUrl": "",
      "clean": false,
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
