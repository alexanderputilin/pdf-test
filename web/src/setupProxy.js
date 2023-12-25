const {createProxyMiddleware} = require('http-proxy-middleware');

module.exports = function (app) {
    app.use(
        createProxyMiddleware('/api', {
            target: 'http://localhost:5230',
            changeOrigin: true,
        })
    );

    app.use(
        createProxyMiddleware('/api/notifications', {
            target: 'ws://localhost:5230',
            ws: true,
            changeOrigin: true,
        })
    );
};
