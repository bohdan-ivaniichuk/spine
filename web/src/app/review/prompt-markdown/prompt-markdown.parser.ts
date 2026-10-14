/**
 * Tiny Markdown subset for question prompts, options and explanations: fenced code blocks
 * (```lang … ```) and `inline code`. Everything else is literal text — the template renders the
 * output through interpolation only, so raw HTML in seed content can never execute.
 */

export interface InlineText {
  kind: 'text';
  text: string;
}

export interface InlineCode {
  kind: 'code';
  text: string;
}

export type Inline = InlineText | InlineCode;

export interface ParagraphBlock {
  kind: 'paragraph';
  inlines: Inline[];
}

export interface CodeBlock {
  kind: 'code';
  language: string;
  code: string;
}

export type Block = ParagraphBlock | CodeBlock;

const FENCE = /^\s*```(.*)$/;

export function parsePromptMarkdown(source: string): Block[] {
  const blocks: Block[] = [];
  const lines = source.replace(/\r\n?/g, '\n').split('\n');
  let paragraph: string[] = [];

  const flushParagraph = () => {
    if (paragraph.length > 0) {
      blocks.push({ kind: 'paragraph', inlines: parseInlines(paragraph.join('\n')) });
      paragraph = [];
    }
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const fence = FENCE.exec(line);
    if (fence) {
      flushParagraph();
      const language = fence[1].trim();
      const code: string[] = [];
      i++;
      // An unterminated fence runs to the end of the source (better than dropping the code).
      while (i < lines.length && !/^\s*```\s*$/.test(lines[i])) {
        code.push(lines[i]);
        i++;
      }
      blocks.push({ kind: 'code', language, code: code.join('\n') });
      continue;
    }
    if (line.trim() === '') {
      flushParagraph();
      continue;
    }
    paragraph.push(line.trimEnd());
  }
  flushParagraph();
  return blocks;
}

/** Splits text on single backticks; an unmatched backtick stays literal. */
export function parseInlines(text: string): Inline[] {
  const inlines: Inline[] = [];
  let buffer = '';
  let i = 0;
  while (i < text.length) {
    if (text[i] === '`') {
      const close = text.indexOf('`', i + 1);
      if (close > i + 1) {
        if (buffer) {
          inlines.push({ kind: 'text', text: buffer });
          buffer = '';
        }
        inlines.push({ kind: 'code', text: text.slice(i + 1, close) });
        i = close + 1;
        continue;
      }
    }
    buffer += text[i];
    i++;
  }
  if (buffer) {
    inlines.push({ kind: 'text', text: buffer });
  }
  return inlines;
}
