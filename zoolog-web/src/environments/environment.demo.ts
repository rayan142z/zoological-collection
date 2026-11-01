// Public demo build: no backend. All /api requests are answered in the browser
// by the demo backend interceptor (src/app/demo) with sample data in localStorage.
export const environment = {
    production: true,
    demo: true,
    apiUrl: '/api',
    assetBase: ''
};
