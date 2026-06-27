# crashguard-app

The CrashGuard Admin App — a React/Vite frontend for managing canaries, channels, and settings.

## Running it standalone

```
npm install
npm run dev
```

This starts the Vite dev server (default `http://localhost:5173`).

## Pointing it at the Engine

This app needs a running [crashguard-engine](../crashguard-engine/README.md) to talk to. Unlike the bundled Docker
image — where nginx proxies `/api/` to the engine automatically — running the app standalone means there's no
proxy in front of it, so you must tell it where the engine is.

Set `VITE_API_BASE_URL` to the engine's URL, e.g. in a `.env.local` file in this directory:

```
VITE_API_BASE_URL=http://localhost:5050
```

If unset, it defaults to `''` (relative paths like `/api/canaries`), which only works when something — nginx, a
dev proxy, etc. — is sitting in front of both the app and the engine on the same origin. When running the app on
its own with `npm run dev`, you almost always want `VITE_API_BASE_URL` set explicitly.

This only needs to match whatever port the engine is actually listening on — see
[crashguard-engine's README](../crashguard-engine/README.md#configuring-the-listen-port) for how to configure that.

Note: the engine's CORS policy only allows origins whose host is `localhost` (any port) by default. If you're
serving this app from somewhere other than `localhost`, you'll need to update the CORS policy in
[`Program.cs`](../crashguard-engine/Program.cs).

---

## React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
