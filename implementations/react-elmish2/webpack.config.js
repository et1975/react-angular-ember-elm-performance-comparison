var path = require("path");
var fs = require("fs");
var webpack = require("webpack");
var extractTextPlugin = require("extract-text-webpack-plugin");

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var babelOptions = require("fable-utils").resolveBabelOptions({
  presets: [["es2015", { "modules": false }]],
  plugins: ["transform-runtime"]
})

var out_path = resolve("./public");

module.exports = {
  devtool: "source-map",
  entry: resolve('src/todomvc.fsproj'),
  output: { filename: 'bundle.js',
            path: out_path },
  devServer: {
    publicPath: "/public",
    contentBase: resolve("."),
    port: 8080
  },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: "fable-loader",
          options: {
            babel: babelOptions
          }
        }
      },
      {
        test: /\.js$/,
        exclude: /node_modules[\\\/](?!fable-)/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },
      },
      {
        test: /\.css$/,
        use: [
          "style-loader",
          "css-loader",
        ]
      },
      {
        test: /\.sass$/,
        loader: extractTextPlugin.extract({
          fallbackLoader: "style-loader",
          loader: "css-loader!sass-loader",
        }),
      },
      {
        test: /\.(jpe|jpg|woff|woff2|eot|ttf|svg)(\?.*$|$)/,
        use: {
          loader: 'file-loader',
          query: {
            name: "fonts/[name].[ext]",
            publicPath: "./"
          }
        },
      }
    ]
  },
  resolve: {
    modules: ["node_modules", path.resolve('node_modules')]
  },
  plugins: [ new extractTextPlugin("styles.css") ]
};
