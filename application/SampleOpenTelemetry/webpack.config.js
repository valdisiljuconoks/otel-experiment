const path = require('path');

module.exports = {
    entry: './assets/document-load.js',
    output: {
        filename: 'main.js',
        path: path.resolve(__dirname, 'dist'),
    },
};