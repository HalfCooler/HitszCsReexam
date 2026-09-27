# 生成答案模板

需要 Python 3.10 或更新版本，无第三方依赖。在仓库根目录执行：

```powershell
python src/data/answers/convert_to_json.py
```

默认读取脚本同目录的 `index.md`，覆盖同目录的 `answer.json`。也可以指定路径：

```powershell
python src/data/answers/convert_to_json.py src/data/questions/index.md -o answer-template.json
```

- `single_choice` 和 `judgement` 分别保存单项选择题和判断题，`id` 保留各题型内的原题号。
- `answer`、`explain` 始终为 `null`，源文件中的 `[x]` 不会转换成答案。已填写答案后，重新运行请用 `-o` 输出到另一文件。
- `question` 保存首段题干，行内公式、代码等 Markdown 原样保留。
- 后续附加内容保存到 `extra_body`：只有一张独立图片时，`extra` 为 `"image"`，内容为图片路径；代码、表格、多张图片或混合内容时为 `"markdown"`，内容为完整 Markdown；无附加内容时两者均为 `null`。图片路径相对于输入 Markdown 所在目录，脚本不复制图片。
- 选择题中代码块外从 `1.` 开始的连续编号列表视为复合陈述，去掉编号后存入 `multiple_options`，并设置 `multiple: true`；普通单选题分别为 `null` 和 `false`。这里的复合选择题仍只选一个答案，`options` 按顺序对应 `a`、`b`、`c`、`d` 等。
- 代码或表格之后的文字继续保存在 `extra_body`，展示时先渲染 `question`，再渲染附加内容和选项。

遇到重复题号、未闭合代码围栏、异常标题或选项时，脚本报错并保留原输出文件。
