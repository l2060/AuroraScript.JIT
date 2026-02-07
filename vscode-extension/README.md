
AuroraScript的 VSCode插件，提供简单的代码着色和格式化功能。


## 前置

``` bash
npm install -g yo generator-code @vscode/vsce
```


## 打包

``` bash
npm install
npm run compile
vsce package --no-yarn
```


## 发布

``` bash
vsce login <your-publisher-name>
vsce publish
```

