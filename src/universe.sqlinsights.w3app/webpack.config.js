const path = require("path")
const UglifyJsPlugin = require("uglifyjs-webpack-plugin")
const glob = require("glob")

module.exports = {
  entry: {
    "bundle.js": glob.sync("build/static/?(js|css)/main.*.?(js|css)").map(f => path.resolve(__dirname, f)),
  },
  output: {
    filename: "build/static/js/bundle.min.js",
  },
  module: {
    rules: [
      {
        test: /\.css$/,
        use: ["style-loader", "css-loader"],
      },
    ],
  },
  plugins: [
    new UglifyJsPlugin({
        parallel: true,
        sourceMap: true,
    }), 
    /* "webpack-react-component-name": "^4.0.5" */
    new WebpackReactComponentNamePlugin222({include: () => true})
  ],
}
