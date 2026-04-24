import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, "../../", "");
  const apiBaseUrl = env.VITE_API_BASE_URL || env.API_BASE_URL || "http://localhost:5282";

  return {
    plugins: [react()],
    envDir: "../../",
    define: {
      __API_BASE_URL__: JSON.stringify(apiBaseUrl),
    },
    server: {
      port: 5173,
    },
  };
});
