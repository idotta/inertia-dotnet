import { createRoot } from 'react-dom/client';
import { createInertiaApp } from '@inertiajs/react';

createInertiaApp({
    resolve: name => {
        const pages = import.meta.glob('./Pages/**/*.jsx', { eager: true });
        const page = pages[`./Pages/${name}.jsx`];
        
        // Automatically use Layout if the page doesn't have one
        if (page.default.layout === undefined) {
            const Layout = require('./Shared/Layout').default;
            page.default.layout = page => <Layout>{page}</Layout>;
        }
        
        return page;
    },
    setup({ el, App, props }) {
        createRoot(el).render(<App {...props} />);
    },
});
