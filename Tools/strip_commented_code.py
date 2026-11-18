"""Remove commented-out code blocks from C# files (#10).

A block = consecutive lines whose first non-space chars are '//' (not '///').
A block is removed when at least 60% of its non-empty lines look like code.
Prose comments, /// doc comments, /* */ section headers and trailing comments are kept.

Usage: python Tools/strip_commented_code.py <file-list.txt> [--dry-run]
"""
import re
import sys


CODE_START = re.compile(r'^(if|else|for|foreach|while|return|var|public|private|protected|internal|static|new|case|break|continue|switch|using|yield|try|catch|float|int|bool|string|void)\b|^[A-Za-z_][\w<>\[\].]*\s*(=|\+=|-=|\+\+|--)|^[A-Za-z_][\w.<>]*\(')


def is_comment(line):
    s = line.lstrip()
    return s.startswith('//') and not s.startswith('///')


def looks_like_code(body):
    b = body.strip()
    if not b:
        return None  # neutral
    words = len(b.split())
    if re.search(r'[;{}]\s*$', b):
        return True
    if re.search(r'[),]\s*$', b):
        # short call/argument lines are code; long sentences that happen to end in ')' or ',' are prose
        return words <= 8
    # keyword-led lines are code only when short and not sentence-like
    return bool(CODE_START.search(b)) and words <= 6 and not b.endswith('.')


def strip(lines):
    out, removed, i = [], 0, 0
    while i < len(lines):
        if not is_comment(lines[i]):
            out.append(lines[i]); i += 1; continue
        j = i
        while j < len(lines) and is_comment(lines[j]):
            j += 1
        block = lines[i:j]
        votes = [looks_like_code(l.lstrip()[2:]) for l in block]
        votes = [v for v in votes if v is not None]
        if votes and sum(votes) / len(votes) >= 0.6:
            removed += len(block)
            # avoid leaving a double blank line where the block was
            if out and not out[-1].strip() and j < len(lines) and not lines[j].strip():
                j += 1
        else:
            out.extend(block)
        i = j
    return out, removed


def main():
    files = [l.strip() for l in open(sys.argv[1], encoding='utf-8') if l.strip()]
    dry = '--dry-run' in sys.argv
    total = 0
    for f in files:
        raw = open(f, encoding='utf-8-sig').read()
        newline = '\r\n' if '\r\n' in raw else '\n'
        lines = raw.split(newline)
        out, removed = strip(lines)
        if removed:
            total += removed
            print(f'{removed:4d}  {f}')
            if not dry:
                open(f, 'w', encoding='utf-8', newline='').write(newline.join(out))
    print(f'total removed: {total}')


if __name__ == '__main__':
    main()
