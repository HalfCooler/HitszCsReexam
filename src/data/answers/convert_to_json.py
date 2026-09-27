"""将题库 Markdown 转换为待填写答案的 JSON（仅使用 Python 标准库）。"""

import argparse
import json
from pathlib import Path
import re
import string


SECTIONS = {"单项选择": "single_choice", "判断": "judgement"}
HEADING = re.compile(r"^###\s+(\d+)\.\s*[（(]\d+分[）)]\s*$")
OPTION = re.compile(r"^\s*[-*+] \[[ xX]\]\s+(.*)$")
NUMBERED = re.compile(r"^\s*(\d+)[.、)]\s+(.+)$")
FENCE = re.compile(r"^\s*(`{3,}|~{3,})(.*)$")
IMAGE = re.compile(r'!\[[^\]]*\]\(([^\s]+?)(?:\s+"[^"]*")?\)')


def outside_fences(lines):
    """标记代码围栏之外的行，避免把代码中的题号、选项或编号当成结构。"""
    fence = None
    for line in lines:
        marker = FENCE.match(line)
        outside = fence is None
        if marker:
            token, suffix = marker.groups()
            if fence is None:
                fence = token
            elif token[0] == fence[0] and len(token) >= len(fence) and not suffix.strip():
                fence = None
        yield line, outside and marker is None
    if fence is not None:
        raise ValueError("存在未闭合的代码围栏")


def split_stem(lines, compound_allowed):
    stem = []
    statements = []
    for line, outside in outside_fences(lines):
        item = NUMBERED.match(line) if outside and compound_allowed else None
        if item:
            if int(item[1]) != len(statements) + 1:
                raise ValueError("复合选项的编号必须从 1 开始连续递增")
            statements.append(item[2])
        elif statements and line.startswith(("    ", "\t")) and outside:
            statements[-1] += "\n" + line.strip()
        else:
            stem.append(line)

    # 首段作为题干；后续内容整体保留，使代码后面的提问不会丢失或乱序。
    paragraphs = re.split(r"\n\s*\n", "\n".join(stem).strip(), maxsplit=1)
    question = paragraphs[0]
    if not question:
        raise ValueError("题干不能为空")
    extra_body = paragraphs[1].strip() if len(paragraphs) > 1 else None
    extra_body = extra_body or None
    extra = "markdown" if extra_body else None
    if extra_body and (image := IMAGE.fullmatch(extra_body)):
        extra = "image"
        extra_body = image[1]
    return question, extra, extra_body, statements


def make_question(kind, number, lines):
    stem = []
    options = []
    for line, outside in outside_fences(lines):
        option = OPTION.match(line) if outside else None
        if option:
            options.append([option[1]])
        elif options:
            options[-1].append(line)
        else:
            stem.append(line)
    options = ["\n".join(option).strip() for option in options]
    question, extra, extra_body, statements = split_stem(stem, kind == "single_choice")
    result = {
        "id": number,
        "question": question,
        "extra": extra,
        "extra_body": extra_body,
    }
    if kind == "single_choice":
        if not 2 <= len(options) <= len(string.ascii_lowercase) or not all(options):
            raise ValueError("选择题必须包含 2 至 26 个非空选项")
        result.update(
            multiple=bool(statements),
            multiple_options=statements or None,
            options=dict(zip(string.ascii_lowercase, options)),
        )
    elif options != ["是", "否"]:
        raise ValueError("判断题的选项必须依次为“是”和“否”")
    # 勾选状态不代表本次转换的答案，所有题目均初始化为待填写状态。
    result.update(answer=None, explain=None)
    return result


def convert(markdown):
    result = {kind: [] for kind in SECTIONS.values()}
    seen = {kind: set() for kind in SECTIONS.values()}
    kind = None
    number = None
    body = []

    def commit():
        if number is None:
            return
        if number in seen[kind]:
            raise ValueError(f"{kind} 第 {number} 题：题号重复")
        try:
            item = make_question(kind, number, body)
        except ValueError as error:
            raise ValueError(f"{kind} 第 {number} 题：{error}") from error
        seen[kind].add(number)
        result[kind].append(item)

    for line, outside in outside_fences(markdown.splitlines()):
        if outside and line.startswith("## "):
            commit()
            title = line[3:].strip()
            if title not in SECTIONS:
                raise ValueError(f"不支持的题型：{title}")
            kind, number, body = SECTIONS[title], None, []
        elif outside and (heading := HEADING.fullmatch(line)):
            commit()
            if kind is None:
                raise ValueError("题目前缺少题型标题")
            number, body = int(heading[1]), []
        elif outside and line.startswith("### "):
            raise ValueError(f"无法识别题目标题：{line}")
        elif number is not None:
            body.append(line)
    commit()
    if not any(result.values()):
        raise ValueError("未找到任何题目")
    return result


def main():
    directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", nargs="?", type=Path, default=directory / "index.md",
                        help="输入 Markdown，默认使用脚本同目录的 index.md")
    parser.add_argument("-o", "--output", type=Path, default=directory / "answer.json",
                        help="输出 JSON，默认使用脚本同目录的 answer.json（会覆盖）")
    args = parser.parse_args()
    try:
        if args.input.resolve() == args.output.resolve():
            raise ValueError("输入与输出不能是同一个文件")
        result = convert(args.input.read_text(encoding="utf-8-sig"))
        # 全部解析成功后才写入，解析失败时保留原输出。
        args.output.write_text(json.dumps(result, ensure_ascii=False, indent=4) + "\n",
                               encoding="utf-8")
    except (OSError, ValueError) as error:
        parser.exit(1, f"转换失败：{error}\n")
    print(f"已生成 {args.output}：单选 {len(result['single_choice'])} 题，"
          f"判断 {len(result['judgement'])} 题，"
          f"复合选择 {sum(q['multiple'] for q in result['single_choice'])} 题。")


if __name__ == "__main__":
    main()
