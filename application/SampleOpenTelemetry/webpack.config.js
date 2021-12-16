const path = require('path');

module.exports = {
    entry: './assets/site.js',
    output: {
        filename: 'main.js',
        path: path.resolve(__dirname, 'dist')
    }
};