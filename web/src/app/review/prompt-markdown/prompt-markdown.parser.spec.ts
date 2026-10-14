import { parseInlines, parsePromptMarkdown } from './prompt-markdown.parser';

describe('parsePromptMarkdown', () => {
  it('returns no blocks for an empty string', () => {
    expect(parsePromptMarkdown('')).toEqual([]);
    expect(parsePromptMarkdown('\n\n')).toEqual([]);
  });

  it('splits a fenced block with a language tag out of the surrounding paragraphs', () => {
    const blocks = parsePromptMarkdown(
      'What does this print?\n\n```csharp\nSpan<int> s = stackalloc int[4];\nConsole.WriteLine(s.Length);\n```\nPick one.',
    );

    expect(blocks).toEqual([
      { kind: 'paragraph', inlines: [{ kind: 'text', text: 'What does this print?' }] },
      {
        kind: 'code',
        language: 'csharp',
        code: 'Span<int> s = stackalloc int[4];\nConsole.WriteLine(s.Length);',
      },
      { kind: 'paragraph', inlines: [{ kind: 'text', text: 'Pick one.' }] },
    ]);
  });

  it('parses inline code inside a paragraph', () => {
    expect(parsePromptMarkdown('Use `Span<T>` instead of `T[]` here.')).toEqual([
      {
        kind: 'paragraph',
        inlines: [
          { kind: 'text', text: 'Use ' },
          { kind: 'code', text: 'Span<T>' },
          { kind: 'text', text: ' instead of ' },
          { kind: 'code', text: 'T[]' },
          { kind: 'text', text: ' here.' },
        ],
      },
    ]);
  });

  it('treats an unterminated fence as code up to the end', () => {
    expect(parsePromptMarkdown('Intro\n```ts\nconst a = 1;\nconst b = 2;')).toEqual([
      { kind: 'paragraph', inlines: [{ kind: 'text', text: 'Intro' }] },
      { kind: 'code', language: 'ts', code: 'const a = 1;\nconst b = 2;' },
    ]);
  });

  it('keeps raw HTML as literal text', () => {
    const blocks = parsePromptMarkdown('<script>alert(1)</script> and <b>bold</b>');
    expect(blocks).toEqual([
      {
        kind: 'paragraph',
        inlines: [{ kind: 'text', text: '<script>alert(1)</script> and <b>bold</b>' }],
      },
    ]);
  });

  it('joins consecutive lines into one paragraph and splits on blank lines', () => {
    const blocks = parsePromptMarkdown('line one\nline two\n\nsecond paragraph');
    expect(blocks).toHaveLength(2);
    expect(blocks[0]).toEqual({
      kind: 'paragraph',
      inlines: [{ kind: 'text', text: 'line one\nline two' }],
    });
  });
});

describe('parseInlines', () => {
  it('leaves an unmatched backtick literal', () => {
    expect(parseInlines('a ` b')).toEqual([{ kind: 'text', text: 'a ` b' }]);
  });

  it('ignores empty code spans', () => {
    expect(parseInlines('a `` b')).toEqual([{ kind: 'text', text: 'a `` b' }]);
  });
});
