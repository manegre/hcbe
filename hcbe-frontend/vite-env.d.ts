/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_ENABLE_MEMBER_LOGIN?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare const __BASE_PATH__: string;
declare const __IS_PREVIEW__: boolean;
declare const __READDY_PROJECT_ID__: string;
declare const __READDY_VERSION_ID__: string;