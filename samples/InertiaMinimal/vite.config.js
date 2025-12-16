import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    build: {
        outDir: 'wwwroot',
        emptyOutDir: false,
        manifest: true,
        rollupOptions: {
            input: {
                app: 'Resources/js/app.jsx',
                css: 'Resources/css/app.css'
            },
            output: {
                entryFileNames: 'js/[name].js',
                chunkFileNames: 'js/[name].js',
                assetFileNames: 'css/[name].[ext]'
            }
        }
    },
    server: {
        strictPort: true,
        port: 5173,
        hmr: {
            host: 'localhost'
        }
    }
});
